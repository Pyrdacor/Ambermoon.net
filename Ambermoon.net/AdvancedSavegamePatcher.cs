using Ambermoon.Data;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using SonicArranger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ambermoon
{
    internal class AdvancedSavegamePatcher
    {
        class Patch
        {
            // From episode 2 to 3 we use a new diff format.
            enum DiffType : byte
            {
                ByteValueChange,
                WordValueChange,
                BitfieldBitsAdded,
                BitfieldBitsCleared,
                SubfileAdded,
                SubfileRemoved,
                SubfileExtended,
                SubfileShrunk,
                ByteReplacement,
                AddInventoryItem,
                SetSubfile,
            }

            readonly DataReader patchReader;
            int itemIndexHighIndex = -1;
            int itemIndexHighByte = -1;
            int lastChangedItemIndex = -1;
            int maxValueHighIndex = -1;
            int curValueHighIndex = -1;
            int lastAddedTransportIndex = -1;
            record ManualPartyDataChange(int ByteIndex, byte? OldByte, byte? NewByte, byte? Mask = null);

            private static readonly Dictionary<int, List<ManualPartyDataChange>> manualPartyDataChanges = new();

            private static void AddManualChange(int sourceVersion, int targetVersion, ManualPartyDataChange manualChange)
            {
                int key = (targetVersion & 0xf) | ((sourceVersion & 0xf) << 4);

                if (!manualPartyDataChanges.TryGetValue(key, out var changes))
                {
                    manualPartyDataChanges.Add(key, new() { manualChange });
                }
                else
                {
                    changes.Add(manualChange);
                }
            }

            private static void AddManualChangeMask(int sourceVersion, int targetVersion, int byteIndex, byte mask)
            {
                AddManualChange(sourceVersion, targetVersion, new(byteIndex, null, null, mask));
            }

            private static void AddManualChange(int sourceVersion, int targetVersion, int byteIndex, byte oldByte, byte newByte)
            {
                AddManualChange(sourceVersion, targetVersion, new(byteIndex, oldByte, newByte));
            }

            static Patch()
            {
                // Ep 1 to 2

                // Reset glob var 54 (was used for Kasimir before but now for Sunny's brooch)
                AddManualChangeMask(1, 2, 0x104 + 6, 0xbf);
            }

            public Patch(BinaryReader reader)
            {
                int size = (int)(reader.ReadBEUInt32() & int.MaxValue);
                patchReader = new DataReader(reader.ReadBytes(size));
            }

            // This will patch the save files inside the game data
            public void PatchSavegame(ILegacyGameData gameData, int saveSlot, int episodeKey)
            {
                itemIndexHighIndex = -1;
                itemIndexHighByte = -1;
                lastChangedItemIndex = -1;
                maxValueHighIndex = -1;
                curValueHighIndex = -1;
                lastAddedTransportIndex = -1;

                var partyDataChanges = manualPartyDataChanges.GetValueOrDefault(episodeKey, new());

                // Note: the order of SavegameManager.SaveFileNames is important
                // as the patch data is stored in the same order. Also ensure
                // that the first entry is "Party_data.sav".
                patchReader.Position = 0;
                int index = 0;
                foreach (var saveFile in SavegameManager.SaveFileNames)
                {
                    string fullName = $"Save.{saveSlot:00}/{saveFile}";
                    gameData.Files[fullName] = PatchFile(gameData, index++, gameData.Files[fullName], partyDataChanges, episodeKey);
                }
            }

            static void ProcessNewDiffs(int fileIndex, Dictionary<int, DataWriter> subFiles, DataReader diffDataReader)
            {
                int? currentSubFile = null;
                int numActions = diffDataReader.ReadWord();

                while (numActions-- > 0 && diffDataReader.Position < diffDataReader.Size)
                {
                    var action = (DiffType)diffDataReader.ReadByte();

                    switch (action)
                    {
                        case DiffType.ByteValueChange:
                        {
                            currentSubFile ??= 1;
                            int byteIndex = diffDataReader.ReadWord();
                            short change = unchecked((short)diffDataReader.ReadWord());
                            int newValue = subFiles[currentSubFile.Value][byteIndex] + change;
                            byte newByte = newValue > 0 ? (byte)(newValue & 0xff) : unchecked((byte)checked((sbyte)newValue));
                            subFiles[currentSubFile.Value][byteIndex] = newByte;
                            break;
                        }
                        case DiffType.WordValueChange:
                        {
                            currentSubFile ??= 1;
                            int byteIndex = diffDataReader.ReadWord();
                            int change = unchecked((int)diffDataReader.ReadDword());
                            int newValue = ((subFiles[currentSubFile.Value][byteIndex] << 8) | subFiles[currentSubFile.Value][byteIndex + 1]) + change;
                            ushort newWord = newValue > 0 ? (ushort)(newValue & 0xffff) : unchecked((ushort)checked((short)newValue));
                            subFiles[currentSubFile.Value][byteIndex] = (byte)(newWord >> 8);
                            subFiles[currentSubFile.Value][byteIndex + 1] = (byte)(newValue & 0xff);
                            break;
                        }
                        case DiffType.BitfieldBitsAdded:
                        {
                            currentSubFile ??= 1;
                            int byteIndex = diffDataReader.ReadWord();
                            byte bits = diffDataReader.ReadByte();
                            subFiles[currentSubFile.Value][byteIndex] |= bits;
                            break;
                        }
                        case DiffType.BitfieldBitsCleared:
                        {
                            currentSubFile ??= 1;
                            int byteIndex = diffDataReader.ReadWord();
                            byte bits = diffDataReader.ReadByte();
                            subFiles[currentSubFile.Value][byteIndex] &= (byte)~bits;
                            break;
                        }
                        case DiffType.SubfileAdded:
                        {
                            int subfileIndex = diffDataReader.ReadWord();
                            int length = diffDataReader.ReadWord();
                            if (subFiles.TryGetValue(subfileIndex, out var subFile))
                            {
                                if (subFile.Size == 0)
                                    subFiles[subfileIndex] = new DataWriter(diffDataReader.ReadBytes(length));
                                else
                                    throw new AmbermoonException(ExceptionScope.Data, $"Sub file {subfileIndex} was already present but should be added.");
                            }
                            else
                            {
                                subFiles.Add(subfileIndex, new DataWriter(diffDataReader.ReadBytes(length)));
                            }
                            break;
                        }
                        case DiffType.SubfileRemoved:
                        {
                            int subfileIndex = diffDataReader.ReadWord();
                            subFiles.Remove(subfileIndex);
                            break;
                        }
                        case DiffType.SubfileExtended:
                        {
                            int subfileIndex = diffDataReader.ReadWord();
                            int length = diffDataReader.ReadWord();
                            subFiles[subfileIndex].Write(diffDataReader.ReadBytes(length));
                            break;
                        }
                        case DiffType.SubfileShrunk:
                        {
                            int subfileIndex = diffDataReader.ReadWord();
                            int length = diffDataReader.ReadWord();
                            subFiles[subfileIndex].Remove(length, subFiles[subfileIndex].Size - length);
                            break;
                        }
                        case DiffType.ByteReplacement:
                        {
                            currentSubFile ??= 1;
                            int byteIndex = diffDataReader.ReadWord();
                            byte newByte = diffDataReader.ReadByte();
                            subFiles[currentSubFile.Value][byteIndex] = newByte;
                            break;
                        }
                        case DiffType.AddInventoryItem:
                        {
                            currentSubFile ??= 1;
                            byte[] itemSlotData = diffDataReader.ReadBytes(6);
                            int offset = fileIndex == 1 ? 0x158 : 0;

                            for (int i = 0; i < 24; i++)
                            {
                                if (subFiles[currentSubFile.Value][offset + i * 6] == 0)
                                {
                                    for (int j = 0; j < 6; j++)
                                        subFiles[currentSubFile.Value][offset + i * 6 + j] = itemSlotData[j];
                                    break;
                                }
                            }

                            break;
                        }
                        case DiffType.SetSubfile:
                        {
                            currentSubFile = diffDataReader.ReadWord();
                            break;
                        }
                    }
                }
            }

            delegate void PatchMethod(int subFileIndex, int byteIndex, byte oldByte, byte newByte);

            static void ProcessDiffEntry(int fileIndex, Dictionary<int, DataWriter> subFiles, DataReader diffDataReader, PatchMethod patchMethod)
            {
                byte action = diffDataReader.ReadByte();

                int ReadSubFileIndex()
                {
                    int subfileIndex = diffDataReader.ReadWord();

                    if (fileIndex == 0 && subfileIndex == 0)
                        return 1;

                    return subfileIndex;
                }

                switch (action)
                {
                    case 0:
                    {
                        // byte changed
                        int subfileIndex = ReadSubFileIndex();
                        int byteIndex = (int)(diffDataReader.ReadDword() & int.MaxValue);
                        byte oldByte = diffDataReader.ReadByte();
                        byte newByte = diffDataReader.ReadByte();
                        if (!subFiles.ContainsKey(subfileIndex))
                            throw new AmbermoonException(ExceptionScope.Data, $"Sub file {subfileIndex} is not present.");
                        patchMethod(subfileIndex, byteIndex, oldByte, newByte);
                        break;
                    }
                    case 1:
                    {
                        // add bytes
                        int subfileIndex = ReadSubFileIndex();
                        int byteIndex = (int)(diffDataReader.ReadDword() & int.MaxValue);
                        int size = diffDataReader.ReadWord();
                        if (!subFiles.TryGetValue(subfileIndex, out var subFile))
                            throw new AmbermoonException(ExceptionScope.Data, $"Sub file {subfileIndex} is not present.");
                        subFile.Write(diffDataReader.ReadBytes(size));
                        break;
                    }
                    case 2:
                    {
                        // remove bytes
                        int subfileIndex = ReadSubFileIndex();
                        int byteIndex = (int)(diffDataReader.ReadDword() & int.MaxValue);
                        int size = diffDataReader.ReadWord();
                        if (!subFiles.TryGetValue(subfileIndex, out var subFile))
                            throw new AmbermoonException(ExceptionScope.Data, $"Sub file {subfileIndex} is not present.");
                        if (subFile.Size < byteIndex + size)
                            throw new AmbermoonException(ExceptionScope.Data, $"Sub file {subfileIndex} is smaller than expected. The given amount of bytes can't be removed.");
                        subFile.Remove(byteIndex, size);
                        break;
                    }
                    case 3:
                    {
                        // new subfile
                        int subfileIndex = ReadSubFileIndex();
                        int size = (int)(diffDataReader.ReadDword() & int.MaxValue);
                        if (subFiles.TryGetValue(subfileIndex, out var subFile) && subFile.Size > 0)
                            throw new AmbermoonException(ExceptionScope.Data, $"Sub file {subfileIndex} was already present but should be added.");
                        subFiles[subfileIndex] = new DataWriter(diffDataReader.ReadBytes(size));
                        break;
                    }
                    case 4:
                    {
                        // remove subfile
                        int subfileIndex = ReadSubFileIndex();
                        if (!subFiles.TryGetValue(subfileIndex, out var subFile) || subFile.Size == 0 || !subFiles.Remove(subfileIndex))
                            throw new AmbermoonException(ExceptionScope.Data, $"Sub file {subfileIndex} was not present but should be removed.");
                        break;
                    }
                    default:
                    {
                        throw new AmbermoonException(ExceptionScope.Data, $"Invalid patch action: {action}");
                    }
                }
            }

            void PatchByte(IDataReader initialSubFileContent, DataWriter subFileData, int fileIndex, int byteIndex, byte oldByte, byte newByte)
            {
                void AddAssignUByte(DataWriter data, int index, int value)
                {
                    data[index] = (byte)Util.Limit(0, data[index] + value, 255);
                }

                bool checkItemIndex = true;
                var initialSubFileData = initialSubFileContent as DataReader;

                // Changed bytes can use different patch mechanisms based on
                // the nature of the affected value. For example bit fields
                // like event or character bits should be patched by adding
                // or removing single bits while values like character values
                // should be patched by replacing, reducing or increasing them.

                void HandleItemSlot(int itemSectionStart)
                {
                    int offset = (byteIndex - itemSectionStart) % 6;

                    switch (offset)
                    {
                        case 0:
                        {
                            // amount
                            if (initialSubFileData[byteIndex + 4] == subFileData[byteIndex + 4] &&
                                initialSubFileData[byteIndex + 5] == subFileData[byteIndex + 5])
                            {
                                // same item?
                                int change = newByte - oldByte;
                                AddAssignUByte(subFileData, byteIndex, change);
                                if (subFileData[byteIndex] == 0)
                                {
                                    // if new amount is 0, clear the item slot
                                    subFileData[byteIndex + 1] = 0;
                                    subFileData[byteIndex + 2] = 0;
                                    subFileData[byteIndex + 3] = 0;
                                    subFileData[byteIndex + 4] = 0;
                                    subFileData[byteIndex + 5] = 0;
                                }
                            }
                            else
                            {
                                // item changed
                                // we will only touch this if the current item slot is empty or unlimited (amount 0xff for merchants)
                                if (lastChangedItemIndex == byteIndex - offset + 4 || subFileData[byteIndex] == 0 || subFileData[byteIndex] == 255)
                                {
                                    subFileData[byteIndex] = newByte;
                                }
                            }
                            break;
                        }
                        case 1: // charges
                        case 2: // recharge times
                        {
                            if (initialSubFileData[byteIndex - offset + 4] == subFileData[byteIndex - offset + 4] &&
                                initialSubFileData[byteIndex - offset + 5] == subFileData[byteIndex - offset + 5])
                            {
                                // same item?
                                if (subFileData[byteIndex - offset] > 0)
                                {
                                    // Only touch this if the item amount is > 0.
                                    // The item might have been removed (e.g. through an amount change).
                                    if (offset == 1 && (newByte == 255 || oldByte == 255))
                                    {
                                        // special case: unlimited charges -> just replace with new value
                                        subFileData[byteIndex] = newByte;
                                    }
                                    else
                                    {
                                        int change = newByte - oldByte;
                                        AddAssignUByte(subFileData, byteIndex, change);
                                    }
                                }
                            }
                            else
                            {
                                // item changed
                                // we will only touch this if the current item slot is empty or unlimited (amount 0xff for merchants)
                                if (lastChangedItemIndex == byteIndex - offset + 4 || subFileData[byteIndex - offset] == 0 || subFileData[byteIndex - offset] == 255)
                                {
                                    subFileData[byteIndex] = newByte;
                                }
                            }
                            break;
                        }
                        case 3:
                        {
                            // item flags
                            if (initialSubFileData[byteIndex - offset + 4] == subFileData[byteIndex - offset + 4] &&
                                initialSubFileData[byteIndex - offset + 5] == subFileData[byteIndex - offset + 5])
                            {
                                // same item?
                                if (subFileData[byteIndex - offset] > 0)
                                {
                                    // Only touch this if the item amount is > 0.
                                    // The item might have been removed (e.g. through an amount change).
                                    // Never remove flags!
                                    subFileData[byteIndex] |= newByte;
                                }
                            }
                            else
                            {
                                // item changed
                                // we will only touch this if the current item slot is empty or unlimited (amount 0xff for merchants)
                                if (lastChangedItemIndex == byteIndex - offset + 4 || subFileData[byteIndex - offset] == 0 || subFileData[byteIndex - offset] == 255)
                                {
                                    subFileData[byteIndex] = newByte;
                                }
                            }
                            break;
                        }
                        case 4:
                        {
                            // item index high byte
                            itemIndexHighByte = newByte;
                            itemIndexHighIndex = byteIndex;
                            checkItemIndex = false;
                            break;
                        }
                        case 5:
                        {
                            // item index
                            // This must be processed before other changes to the item slot!
                            // Only change the item index if the item slot is an unlimited merchant slot, the slot is empty
                            // or the item index matches the expected old one.

                            if (itemIndexHighIndex >= 0 && itemIndexHighIndex != byteIndex - 1)
                            {
                                throw new AmbermoonException(ExceptionScope.Data, "Invalid item index encoding.");
                            }

                            int currentItemIndex = (subFileData[byteIndex - 1] << 8) | subFileData[byteIndex];
                            int newItemIndex = newByte;

                            if (itemIndexHighByte > 0)
                            {
                                newItemIndex |= (itemIndexHighByte << 8);
                            }

                            if (currentItemIndex != newItemIndex)
                            {
                                if (currentItemIndex == 0 || currentItemIndex == oldByte || subFileData[byteIndex - 5] == 255 || subFileData[byteIndex - 5] == 0)
                                {
                                    if (itemIndexHighByte >= 0)
                                    {
                                        subFileData[byteIndex - 1] = (byte)itemIndexHighByte;
                                    }
                                    subFileData[byteIndex] = newByte;
                                    lastChangedItemIndex = byteIndex - 1;
                                }
                            }

                            itemIndexHighByte = -1;
                            itemIndexHighIndex = -1;

                            break;
                        }
                    }
                }

                switch (fileIndex)
                {
                    case 0:
                    {
                        // Party_data.sav
                        if (byteIndex >= 0x35eb)
                        {
                            // tile changes
                            // Only add new ones!
                            // This should not happen.
                            throw new AmbermoonException(ExceptionScope.Data, "Changing initial tile change events seems like an error.");
                        }
                        else if (byteIndex < 0x44 || byteIndex >= 0x35E4 ||
                                (byteIndex >= 0x3504 && byteIndex < 0x35A4))
                        {
                            // Ignore those as they will always change.
                            return;
                        }
                        else if (byteIndex < 0x104)
                        {
                            // transport locations
                            // Only add new ones!
                            int offset = (byteIndex - 0x44) % 6;
                            if (offset == 0)
                            {
                                if (newByte != 0)
                                {
                                    int index = byteIndex;
                                    while (subFileData[index] != 0 && index < 0x104 - 6)
                                    {
                                        index += 6;
                                    }
                                    if (subFileData[index] == 0)
                                    {
                                        subFileData[index] = (byte)(newByte | 0x80); // mark as "just added" with 0x80
                                        lastAddedTransportIndex = index;
                                    }
                                }
                            }
                            else if (lastAddedTransportIndex >= 0 && (subFileData[lastAddedTransportIndex] >> 7) == 0x01)
                            {
                                subFileData[lastAddedTransportIndex + offset] = newByte;
                            }
                        }
                        else
                        {
                            // bit fields
                            // Global vars
                            // Event bits
                            // Char bits
                            // Chest lock states
                            // Door lock states
                            for (int i = 0; i < 8; ++i)
                            {
                                // Process each bit change
                                int oldBit = (oldByte >> i) & 0x01;
                                int newBit = (newByte >> i) & 0x01;

                                if (oldBit != newBit)
                                {
                                    // Bit changed in diff
                                    if (((subFileData[byteIndex] >> i) & 0x1) == oldBit)
                                    {
                                        if (newBit > 0)
                                        {
                                            subFileData[byteIndex] |= (byte)(1 << i);
                                        }
                                        else
                                        {
                                            subFileData[byteIndex] &= (byte)(~(1 << i) & 0xff);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                    case 1:
                    {
                        // Party_char.amb
                        if (maxValueHighIndex >= 0 && byteIndex != maxValueHighIndex + 1)
                        {
                            // The high byte of a max value was changed but there is no
                            // change to the lower byte. So we have to perform the max
                            // value logic here first.
                            PatchByte(initialSubFileData, subFileData, fileIndex, maxValueHighIndex + 1, subFileData[maxValueHighIndex + 1], subFileData[maxValueHighIndex + 1]);
                        }
                        if (curValueHighIndex >= 0 && byteIndex != curValueHighIndex + 1)
                        {
                            // The high byte of a current value was changed but there is no
                            // change to the lower byte. So we have to perform the current
                            // value logic here first.
                            PatchByte(initialSubFileData, subFileData, fileIndex, curValueHighIndex + 1, subFileData[curValueHighIndex + 1], subFileData[curValueHighIndex + 1]);
                        }
                        if (byteIndex >= 0x122 && byteIndex < 0x1e8)
                        {
                            // equipment and inventory
                            HandleItemSlot(0x122);
                        }
                        else if (byteIndex >= 0x2a && byteIndex < 0xd6)
                        {
                            // attributes and skills and HP and SP
                            int offset = (byteIndex - 0x2a) % (byteIndex < 0xca ? 8 : 6);
                            if (offset < 2)
                            {
                                // current value
                                int change = newByte - oldByte;
                                if (change < 0 && subFileData[byteIndex] < -change && offset == 1)
                                {
                                    if (subFileData[byteIndex - 1] == 0)
                                        throw new AmbermoonException(ExceptionScope.Data, "Invalid character value change.");
                                    subFileData[byteIndex - 1]--;
                                }
                                AddAssignUByte(subFileData, byteIndex, change);
                                if (offset == 0)
                                {
                                    curValueHighIndex = byteIndex;
                                }
                                else
                                {
                                    curValueHighIndex = -1;
                                    int value = (subFileData[byteIndex - 1] << 8) | subFileData[byteIndex];
                                    int maxValue = (subFileData[byteIndex + 1] << 8) | subFileData[byteIndex + 2];
                                    if (value > maxValue)
                                    {
                                        // Limit current value to max value.
                                        subFileData[byteIndex - 1] = subFileData[byteIndex + 1];
                                        subFileData[byteIndex] = subFileData[byteIndex + 2];
                                    }
                                }
                            }
                            else if (offset < 4)
                            {
                                // max value
                                // This should come before a current value change.
                                // But it's also possible that the current value
                                // is not changed so we must ensure that the current
                                // value is not exceeding the max value here.
                                int change = newByte - oldByte;
                                if (change < 0 && subFileData[byteIndex] < -change && offset == 3)
                                {
                                    if (subFileData[byteIndex - 1] == 0)
                                        throw new AmbermoonException(ExceptionScope.Data, "Invalid character value change.");
                                    subFileData[byteIndex - 1]--;
                                }
                                AddAssignUByte(subFileData, byteIndex, change);
                                if (offset == 2)
                                {
                                    maxValueHighIndex = byteIndex;
                                }
                                else
                                {
                                    maxValueHighIndex = -1;
                                    int value = (subFileData[byteIndex - 3] << 8) | subFileData[byteIndex - 2];
                                    int maxValue = (subFileData[byteIndex - 1] << 8) | subFileData[byteIndex];
                                    if (value > maxValue)
                                    {
                                        // Limit current value to max value.
                                        subFileData[byteIndex - 3] = subFileData[byteIndex - 1];
                                        subFileData[byteIndex - 2] = subFileData[byteIndex];
                                    }
                                }
                            }
                            else
                            {
                                // bonus value and pre-exhaustion backup value
                                // Just adjust the value.
                                int change = newByte - oldByte;

                                if (change < 0 && subFileData[byteIndex] < -change && offset % 2 == 1)
                                {
                                    if (subFileData[byteIndex - 1] == 0)
                                        throw new AmbermoonException(ExceptionScope.Data, "Invalid character value change.");
                                    subFileData[byteIndex - 1]--;
                                }
                                
                                AddAssignUByte(subFileData, byteIndex, change);
                            }
                        }
                        else if (byteIndex == 0x04 || // usable spell types
                                 byteIndex == 0x08 || // spoken languages
                                 byteIndex == 0x10 || // spell immunity
                                 byteIndex == 0x12 || // battle flags
                                 byteIndex == 0x13 || // elements
                                 byteIndex == 0x1e || // conditions (1st byte)
                                 byteIndex == 0x1f || // conditions (2nd byte)
                                 (byteIndex >= 0xf2 && byteIndex < 0x10e))
                        {
                            // learned spells
                            // Those are all bit fields.
                            // For now only add but never remove.
                            // Most of them should not be changed at all.
                            subFileData[byteIndex] |= newByte;
                        }
                        else if (byteIndex == 0x05 || // level
                                 byteIndex == 0x11 || // APR
                                 (byteIndex >= 0x14 && byteIndex < 0x1c) || // SLP, TP, Gold, Food
                                 (byteIndex >= 0xd6 && byteIndex < 0xe2) || // def, dmg, magic def/dmg
                                 (byteIndex >= 0x10e && byteIndex < 0x112)) // weight
                        {
                            // Those are all values that should be increased or decreased and not just replaced.
                            int change = newByte - oldByte;

                            if (byteIndex >= 0x14)
                            {
                                if (change < 0 && subFileData[byteIndex] < -change && (byteIndex > 0x10e || byteIndex % 2 != 0))
                                {
                                    if (subFileData[byteIndex - 1] == 0)
                                        throw new AmbermoonException(ExceptionScope.Data, "Invalid character value change.");
                                    subFileData[byteIndex - 1]--;
                                }
                            }

                            AddAssignUByte(subFileData, byteIndex, change);
                        }
                        else
                        {
                            // Everything else is just replaced.
                            subFileData[byteIndex] = newByte;
                        }
                        break;
                    }
                    case 2: // Chest_data.amb
                    case 3: // Merchant_data.amb
                    {
                        if (byteIndex >= 144)
                        {
                            // chest gold or food
                            int change = newByte - oldByte;

                            if (change < 0 && subFileData[byteIndex] < -change && byteIndex % 2 != 0)
                            {
                                if (subFileData[byteIndex - 1] == 0)
                                    throw new AmbermoonException(ExceptionScope.Data, "Invalid chest or merchant value change.");
                                subFileData[byteIndex - 1]--;
                            }

                            AddAssignUByte(subFileData, byteIndex, change);
                        }
                        else
                        {
                            // items
                            HandleItemSlot(0);
                        }
                        break;
                    }
                    case 4:
                    {
                        // Automap.amb
                        // Only add explore
                        while (subFileData.Size <= byteIndex)
                            subFileData.Write(0);

                        subFileData[byteIndex] |= newByte;
                        break;
                    }
                    default:
                    {
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid file index.");
                    }
                }

                if (checkItemIndex && itemIndexHighIndex >= 0)
                {
                    PatchByte(initialSubFileData, subFileData, fileIndex, itemIndexHighIndex + 1, initialSubFileData[itemIndexHighIndex + 1], initialSubFileData[itemIndexHighIndex + 1]);
                    itemIndexHighIndex = -1;
                    itemIndexHighByte = -1;
                }
            }

            IFileContainer PatchFile(ILegacyGameData gameData, int fileIndex, IFileContainer oldFile, List<ManualPartyDataChange> partyDataChanges, int episodeKey)
            {
                int size = patchReader.ReadBEInt16();
                var reader = new DataReader(patchReader.ReadBytes(size));
                var subFiles = oldFile.Files.ToDictionary(f => f.Key, f => new DataWriter(f.Value.ToArray()));
                int lastSubFileIndex = -1;

                if (episodeKey != 0x12)
                {
                    ProcessNewDiffs(fileIndex, subFiles, reader);
                }
                else
                {
                    while (reader.Position < reader.Size)
                    {
                        ProcessDiffEntry(fileIndex, subFiles, reader, (subFileIndex, byteIndex, oldByte, newByte) =>
                        {
                            lastSubFileIndex = fileIndex == 0 && subFileIndex == 0 ? 1 : subFileIndex;
                            PatchByte(oldFile.Files[subFileIndex], subFiles[subFileIndex], fileIndex, byteIndex, oldByte, newByte);
                        });
                    }

                    if (itemIndexHighIndex >= 0)
                    {
                        byte value = (oldFile.Files[lastSubFileIndex] as DataReader)[itemIndexHighIndex + 1];
                        PatchByte(oldFile.Files[lastSubFileIndex], subFiles[lastSubFileIndex], fileIndex, itemIndexHighIndex + 1, value, value);
                        itemIndexHighIndex = -1;
                        itemIndexHighByte = -1;
                    }

                    if (maxValueHighIndex >= 0)
                    {
                        // The high byte of a max value was changed but there is no
                        // change to the lower byte. So we have to perform the max
                        // value logic here.
                        byte value = (oldFile.Files[lastSubFileIndex] as DataReader)[maxValueHighIndex + 1];
                        PatchByte(oldFile.Files[lastSubFileIndex], subFiles[lastSubFileIndex], fileIndex, maxValueHighIndex + 1, value, value);
                    }
                    if (curValueHighIndex >= 0)
                    {
                        // The high byte of a current value was changed but there is no
                        // change to the lower byte. So we have to perform the current
                        // value logic here.
                        byte value = (oldFile.Files[lastSubFileIndex] as DataReader)[curValueHighIndex + 1];
                        PatchByte(oldFile.Files[lastSubFileIndex], subFiles[lastSubFileIndex], fileIndex, curValueHighIndex + 1, value, value);
                    }
                }

                if (fileIndex == 0)
                {
                    // Party_data.sav
                    var data = subFiles[1];

                    if (episodeKey == 0x12)
                    {                        
                        // Fix the transport locations
                        int index = 0x44;
                        for (int i = 0; i < 32; ++i)
                        {
                            if ((data[index] & 0x80) != 0)
                            {
                                if (data[index + 1] == 0 || data[index + 2] == 0 || (data[index + 4] == 0 && data[index + 3] == 0))
                                {
                                    // Invalid entry
                                    throw new AmbermoonException(ExceptionScope.Data, "Invalid added transport location.");
                                }
                                data[index] &= 0x7f; // remove the marker
                            }
                        }
                    }
                    else if ((episodeKey & 0x03) == 0x03) // upgrade to ep 3
                    {
                        // Spawn Aman in WEAPONS CHAMBER if the event was already triggered
                        // If event bit 265:4 inactive and glob var 376 is 0, set character bit 407:4 to show and 264:3 to hide
                        if ((data[0x504 + 264 * 8] & 0x08) != 0 && (data[0x104 + 376 / 8] & 0x01) == 0)
                        {
                            data[0x2504 + 406 * 4] &= 0xf7;
                            data[0x2504 + 263 * 4] |= 0x04;
                        }

                        // Remove all existing tile change events which target AA3 (map 368)
                        List<int> tileChangeEventsToRemove = new();
                        for (int i = 0x35EB; i < data.Size; i += 6)
                        {
                            if (data[i] == 0x01 && data[i + 1] == 0x70)
                            {
                                tileChangeEventsToRemove.Add(i);
                            }
                        }
                        for (int i = tileChangeEventsToRemove.Count - 1; i >= 0; --i)
                        {
                            data.Remove(tileChangeEventsToRemove[i], 6);
                        }
                    }

                    foreach (var change in partyDataChanges)
                    {
                        if (change.Mask != null)
                            data[change.ByteIndex] &= change.Mask.Value;
                        else if (data[change.ByteIndex] == change.OldByte)
                            data[change.ByteIndex] = change.NewByte ?? throw new AmbermoonException(ExceptionScope.Data, "Invalid manual party data change.");
                    }
                }
                else if (fileIndex == 1)
                {
                    // Party_char.amb
                    subFiles.ToList().ForEach(subFileEntry => {
                        var subFileIndex = subFileEntry.Key;
                        var subFile = subFileEntry.Value;
                        // Note: We won't calculate values depending on per level values as this
                        // should be handled by the diff automatically.
                        // However we will re-calculate the APR value for all besides animals and cheat chars.
                        if ((subFileIndex == 1 || subFileIndex > 6) && subFile[2] <= 8)
                        {
                            var aprPerLevel = (subFile[0xe2] << 8) | subFile[0xe3];
                            subFile[0x11] = (byte)(aprPerLevel == 0 ? 1 : Math.Min(255, 1 + subFile[0x05] / aprPerLevel));
                        }
                        // Adjust free hands and fingers
                        byte freeHands = 2;
                        byte freeFingers = 2;
                        int index = 0x122 + 3 * 6 + 4; // item index of right hand slot
                        if (subFile[index] != 0 || subFile[index + 1] != 0)
                            --freeHands;
                        index += 12; // goto left hand slot
                        if (subFile[index] != 0 || subFile[index + 1] != 0 || subFile[index - 4] != 0)
                            --freeHands;
                        index += 6; // goto right finger slot
                        if (subFile[index] != 0 || subFile[index + 1] != 0)
                            --freeFingers;
                        index += 12; // goto left finger slot
                        if (subFile[index] != 0 || subFile[index + 1] != 0)
                            --freeFingers;
                        subFile[0x06] = freeHands;
                        subFile[0x07] = freeFingers;

                        uint weight = 0;
                        uint oldWeight = subFile[0x10e];
                        oldWeight <<= 8;
                        oldWeight |= subFile[0x10f];
                        oldWeight <<= 8;
                        oldWeight |= subFile[0x110];
                        oldWeight <<= 8;
                        oldWeight |= subFile[0x111];

                        // Add gold and food
                        weight += ((uint)subFile[0x18] << 8) * Character.GoldWeight;
                        weight += subFile[0x19] * Character.GoldWeight;
                        weight += ((uint)subFile[0x1A] << 8) * Character.FoodWeight;
                        weight += subFile[0x1B] * Character.FoodWeight;

                        for (int i = 0x122; i < 0x1e8; i += 6)
                        {
                            if (subFile[i] != 0)
                            {
                                int itemIndex = (subFile[i + 4] << 8) | subFile[i + 5];
                                
                                if (itemIndex == 0)
                                    continue;

                                weight += subFile[i] * gameData.ItemManager.GetItem((uint)itemIndex).Weight;
                            }
                        }

                        // we fix the weight value but only if it is actually
                        // smaller than the old value. This is to prevent
                        // overweight for old savegames.
                        if (oldWeight > weight)
                        {
                            subFile[0x10e] = (byte)((weight >> 24) & 0xff);
                            subFile[0x10f] = (byte)((weight >> 16) & 0xff);
                            subFile[0x110] = (byte)((weight >> 8) & 0xff);
                            subFile[0x111] = (byte)(weight & 0xff);
                        }
                    });
                }

                return FileReader.CreateRawContainer(oldFile.Name, subFiles.ToDictionary(file => file.Key, file => file.Value.ToArray()));
            }
        }

        readonly Dictionary<byte, Patch> patches = new();

        public AdvancedSavegamePatcher(BinaryReader advancedDiffsReader)
        {
            int patchCount = advancedDiffsReader.ReadByte();

            for (int i = 0; i < patchCount; ++i)
            {
                byte key = advancedDiffsReader.ReadByte();

                patches[key] = new Patch(advancedDiffsReader);
            }
        }

        public void PatchSavegame(ILegacyGameData gameData, int saveSlot, int sourceEpisode, int targetEpisode)
        {
            byte header = (byte)((sourceEpisode << 4) | targetEpisode);

            if (patches.TryGetValue(header, out var patch))
            {
                patch.PatchSavegame(gameData, saveSlot, header);
            }
            else
            {
                // Is there any patch where the target is the expected version?
                // If so pick the one with the lowest source version which is >= the given source version.
                var intermediatePatch = patches.Where(p => (p.Key & 0x0f) == targetEpisode).OrderBy(p => p.Key >> 4).Where(p => (p.Key >> 4) >= sourceEpisode).FirstOrDefault();

                if (intermediatePatch.Value != null)
                {
                    // First try to patch up to the intermediate version.
                    PatchSavegame(gameData, saveSlot, sourceEpisode, intermediatePatch.Key >> 4);
                    // Then patch from there to the target version.
                    intermediatePatch.Value.PatchSavegame(gameData, saveSlot, header);
                }
                else
                {
                    throw new AmbermoonException(ExceptionScope.Data, "No patch information for old Ambermoon Advanced savegame found.");
                }
            }
        }
    }
}

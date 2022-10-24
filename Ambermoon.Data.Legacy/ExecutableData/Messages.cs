using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// All kind of game messages.
    /// 
    /// They follow after the <see cref="WorldNames"/>.
    /// 
    /// There are two text chunks. The first one can
    /// contain placeholders.
    /// 
    /// First chunk:
    /// ============
    /// 
    /// The messages are stored as sections. A section can contain
    /// just null-terminated texts after each other or a
    /// offset section. These sections are used for split
    /// texts that are filled with values at runtime.
    /// 
    /// An offset section starts with a word-aligned 0-longword which
    /// must be skipped then. I also found some additional 0-longwords.
    /// They should be skipped as well.
    /// 
    /// Each offset entry contains 8 bytes. First 4 bytes are the absolute
    /// offset to the text string inside the data hunk. The last 4 bytes
    /// seem to be always 0. Maybe they can adjust the index of the value
    /// to insert/replace? The resulting string is produced by reading the
    /// partial strings at all offsets and concatenate them. Between each
    /// of the partial strings there will be a value provide by the game
    /// at runtime. So you can add C# format placeholders like {0} there.
    /// 
    /// I am not sure yet if I understand this encoding correctly
    /// but it works for now to parse all messages.
    /// 
    /// Second chunk:
    /// =============
    /// 
    /// The second chunk starts with the size of entries (should be 300)
    /// as a dword. Then this amount of words follow which give the
    /// lengths of each entry.
    /// 
    /// Then the entries follow which are plain texts. They are null-
    /// terminated in most cases but not always so use the length to
    /// read them and trim the terminating nulls.
    /// </summary>
    public class Messages
    {
        public enum Index
        {
            None,
            IOErrorOccured,
            InsertDisk,
            InsertSaveDisk,
            TextBlockMissing,
            DeactivateMusicLackOfMem,
            RestartOutOfMem,            
            DontForgetItems = 18,
            Comma,
            FullStop,
            HasNoMoreFood,
            RegainsLP,
            RegainsLPAndSP,
            HasAged,
            HasDiedOfAge,
            AgreeOnPrice,
            AgreeOnFoodPrice,
            HasReachedLevel,
            AttacksWith,
            Attacks,
            WasBroken,
            DidPointsOfDamage,
            ReceiveExp,
            CastsSpell,
            CastsSpellFrom,
            IsNotTheRightAnswer,
            That,
            NothingToRespond, // = 38, in original code this is the first message so subtract 38 and you have the message id there
            OSOutOfMemory,
            FileInUse,
            FileAlreadyExists,
            DirectoryNotFound,
            FileNotFound,
            FileTooLarge,
            InvalidFilename,
            ObjectNotOfRequiredType,
            DiskNotValidated,
            Empty,
            SeekError,
            DiskFull,
            IncompleteRead,
            FileIsWriteProtected,
            FileIsReadProtected,
            NotADosDisk,
            IncompleteWrite,
            EnterBattlePositions,
            WhichScrollToRead,
            ThatsNotASpellScroll,
            CantLearnSpellsOfType,
            ManagedToLearnSpell,
            FailedToLearnSpell,
            PartyRestsFor8Hours,
            NotEnoughSpellLearningPoints,
            NotEnoughSP,
            WrongArea,
            WhichItemToDrop,
            YouNeedThisItem,
            WhichItemToUse,
            WhichItemToExamine,
            DropHowMuchGold,
            DropHowMuchFood,
            ThisCannotBeEquipped,
            WrongClassToEquip,
            WrongSexToEquip,
            NotEnoughFreeHands,
            NotEnoughFreeFingers,
            RestingTooDangerous,
            DropHowMany,
            CannotUseMagicDiscHere,
            ThisItemIsCursed,
            ThisItemFulfillsSpecialPurpose,
            WhomGiveItTo,
            CannotEquipInCombat,
            BlowsTheFlute,
            ReallyDropIt,
            SameItemAlreadyInUse,
            ReallyDropGold,
            ReallyDropFood,
            DarkDontFindWayBack,
            GiveHowMuchGold,
            GiveHowMuchFood,
            TakeHowMany,
            WhereToMoveItTo,
            CannotUnequipInCombat,
            ReviveMessage,
            SeeRoundDiskInFloor,
            YouNoticeATrap,
            ReallyQuit,
            AlreadyKnowsSpell,
            CannotUseBrokenItems,
            CannotUseItHere,
            YouLevitate,
            WhichNumber,
            CannotJumpThroughWalls,
            WhichMemberShouldBeBlinked,
            HasOpenedDoor,
            HearStrangeSound,
            HasUnlockedDoor,
            DiscoverTrap,
            DoesNotDiscoverTraps,
            DisarmTrap,
            UnlockedDoorWithLockpick,
            LockpickBreaks,
            UnableToPickTheLock,
            WhichItemToOpenDoor,
            WhichItemToOpenChest,
            HasUnlockedChest,
            HasOpenedChest,
            UnlockedChestWithLockpick,
            ThisItemDoesNotOpenDoor,
            ThisItemDoesNotOpenChest,
            WhichItemToPutInChest,
            StoreHowMany,
            StoreHowMuchGold,
            StoreHowMuchFood,
            NoMoreGoldFitsIntoChest,
            NoMoreFoodFitsIntoChest,
            NoOneCanCarryThatMuch,
            ChestFull,
            ChestNowFull,
            ReadWriteError,
            ReallyLoad,
            ReallyOverwriteSave,
            SaveWhichSavegame,
            DenyLeavingPartyOnForestMoon,
            CannotSendDeadPeopleAway,
            CrazyPeopleDontFollowCommands,
            PetrifiedPeopleCantGoHome,
            SleepUntilDawn,
            HowAboutSomeMore,
            WhoToTalkTo,
            SelfTalkingIsMad,
            ThisPersonIsAsleep,
            YouDontSpeakSameLanguage,
            DontKnowAnythingSpecialAboutIt,
            WhichItemToShow,
            WhichItemToGive,
            GiveHowMuchGoldToNPC,
            GiveHowMuchFoodToNPC,
            NotInterestedInGold,
            NotInterestedInFood,
            NotInterestedInItem,
            DenyJoiningParty,
            WellShouldLeave,
            Hello,
            GoodBye,
            YesWhatIsIt,
            YouCantConversate,
            PartyFull,
            WelcomeCriticalHitTrainer,
            WelcomeHealer,
            WelcomeSage,
            WelcomeRecharger,
            WelcomeInnkeeper,
            WelcomeMerchant,
            WelcomeFoodDealer,
            WelcomeMagician,
            WelcomeRaftSeller,
            WelcomeShipSeller,
            WelcomeHorseSeller,
            LootAfterBattle,
            NoOneReceivesExp,
            WhichItemToSell,
            SellHowMany,
            WayBackTooDangerous,
            PleaseRemoveWriteProtection,
            CannotCarryAllGold,
            ForThisIllGiveYou,
            ReallyWantToGoThere,
            Flees,
            AlreadyAtGotoPoint,
            GotoPointSaved,
            CannotBuyAnymore,
            WhichItemToBuy,
            BuyHowMany,
            ThisWillCost,
            WhichMerchantItemToExamine,
            NotEnoughMoney,
            StayWillCost,
            PriceForRaft,
            PriceForShip,
            PriceForHorse,
            WhichItemToEnchant,
            MoveHowMuchFood,
            BuyHowMuchFood,
            PriceOfFood,
            FoodLeftAfterDividing,
            FoodDividedEqually,
            WantToLeaveRestOfFood,
            LeaveBoughtGoods,
            CannotEnchantOrdinaryItem,
            AlreadyFullyCharged,
            CannotRechargeAnymore,
            PriceForEnchanting,
            LastTimeEnchanting,
            HowManyCharges,
            WhichItemToExamineSage,
            ItemAlreadyIdentified,
            PriceForExamining,
            HowManyLP,
            PriceForHealing,
            WhichConditionToHeal,
            CompletelyExhausted,
            RestingWouldHaveNoEffect,
            PriceForHealingCondition,
            PriceForRemovingCurses,
            TrainHowOften,
            PriceForTraining,
            NotEnoughTrainingPoints,
            IncreasedAfterTraining,
            WelcomeAttackTrainer,
            WelcomeParryTrainer,
            WelcomeSwimTrainer,
            WelcomeFindTrapTrainer,
            WelcomeDisarmTrapTrainer,
            WelcomeLockPickingTrainer,
            WelcomeSearchTrainer,
            WelcomeReadMagicTrainer,
            WelcomeUseMagicTrainer,
            Empty1,
            LPAreNow,
            SPAreNow,
            SLPAreNow,
            TPAreNow,
            APRAreNow,
            NextLevelAt,
            MaxLevelReached,
            AttackWantToFight,
            WhereToMoveTo,
            NowhereToMoveTo,
            CouldNotEscape,
            NoAmmunition,
            WhatToAttack,
            CannotReachAnyone,
            ItemIsBroken,
            WelcomeBlacksmith,
            WhichItemToRepair,
            WontBuyBrokenStuff,
            MissedTheTarget,
            CannotPenetrateMagicalAura,
            AttackFailed,
            AttackWasDeflected,
            AttackDidNoDamage,
            MadeCriticalHit,
            UsedLastAmmunition,
            CannotMove,
            TooFarAway,
            UnableToAttack,
            SomeoneAlreadyGoingThere,
            SelectNewLeader,
            Empty2,
            GettingTired,
            WaitHowManyHours,
            MonstersAdvance,
            MoreFoodWouldBeGood,
            Moves,
            WayWasBlocked,
            LoadWhichSavegame,
            HasDroppedWeapon,
            Retreats,
            PartyAdvances,
            Options,
            WhichPartyMemberAsTarget,
            WhichMonsterAsTarget,
            WhichInventoryAsTarget,
            WhichPartyMemberRowAsTarget,
            WhichMonsterRowAsTarget,
            WhichItemAsTarget,
            WrongWorld,
            SpellFailed,
            WrongClassToUseItem,
            WrongPlaceToUseItem,
            WrongWorldToUseItem,
            DeflectedSpell,
            ImmuneToSpellType,
            TheSpellFailed,
            IsNotDead,
            CannotBeResurrected,
            IsNotAsh,
            IsNotDust,
            AshesChangedToBody,
            DustChangedToAshes,
            AshesFallToDust,
            BodyBurnsUp,
            ItemAlreadyFullyCharged,
            UseSpellOnlyInCitiesOrDungeons,
            MarksPosition,
            ReturnToMarkedPosition,
            HasntMarkedAPosition,
            CannotDamagePetrifiedMonsters,
            ImmuneToSpell,
            ThisIsNotAMagicalItem,
            NoChargesLeft,
            WhereToBlinkTo,
            HasBlinked,
            CannotBlink,
            CannotBeDuplicated,
            CannotClimbHere,
            ItemIsNotBroken,
            NoRoomForItem,
            NoCursedItemFound,
            CannotRepairUnbreakableItem,
            PriceForRepair,
            MapViewNotWorkingHere,
            HappyWithCharacter,
            YouDontKnowAnySpellsYet,
            EscapedTheTrap,
            CannotParry,
            InnkeeperGoodSleepWish,
            ItemCannotBeUsedHere,
            CannotCallEagleIfNotOnFoot,
            UseItOnWhom,
            NotInterestedInTrinket,
            NotAllowingToLookIntoBackpack,
            DontWantToTakeItemsWithYou,
            WaitingIsTooDangerous,
            AutomapperNotWorkingHere,
            GameOver,
            GameOverLoadOrQuit,
            NoSavegamesYetOnlyInitialGame,
            DontDeleteSavedGames,
            TurnOnTuneInAndDropOut,
            // Ambermoon Advanced
            ReviveCat = 338,
            CannotExchangeExpWithAnimals,
            Count
        }

        readonly List<string> entries = new List<string>();
        public IReadOnlyList<string> Entries => entries.AsReadOnly();
        public string GetEntry(Index index) => entries[(int)index];
        static readonly Regex PlaceHolderRegex = new Regex(@"\{[0-9]\}", RegexOptions.Compiled);

        internal Messages(List<string> formatMessages, List<string> messages)
        {
            if (formatMessages.Count != 26 || messages.Count < 300)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid number of messages.");

            entries.Add(""); // None
            entries.AddRange(formatMessages.Take(6));
            entries.AddRange(Enumerable.Repeat("", 11));
            entries.AddRange(formatMessages.Skip(6).Select(FixMessage));
            entries.AddRange(messages);

            while (entries.Count < (int)Index.Count)
                entries.Add("");
        }

        static string FixMessage(string message)
        {
            // The new Text.amb often prepends a format
            // placeholder for the subject like "{0}'s weapon was broken!".
            // In the old loader from the executable those placeholders
            // were not added and the remake code is based on that. So if
            // we encounter a placeholder at the start of the message, we
            // remove it and adjust the following placeholder indices.
            if (message.StartsWith("{0}"))
            {
                message = message.Substring(3);

                var matches = PlaceHolderRegex.Matches(message);

                for (int i = 0; i < matches.Count; ++i)
                {
                    var match = matches[i];
                    char c = match.Value[1];
                    message = message[..(match.Index + 1)] + ((char)(c - 1)).ToString() + message[(match.Index + 2)..];
                }
            }

            return message;
        }

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the message sections just behind the
        /// insert disk messages.
        /// 
        /// It will be behind all the message sections after this.
        /// </summary>
        internal Messages(IDataReader dataReader)
        {
            while (ReadText(dataReader))
                ;

            int numTextEntries = (int)dataReader.ReadDword();
            var textEntryLengths = new List<uint>(numTextEntries);

            for (int i = 0; i < numTextEntries; ++i)
                textEntryLengths.Add(dataReader.ReadWord());

            for (int i = 0; i < numTextEntries; ++i)
                entries.Add(dataReader.ReadString((int)textEntryLengths[i], AmigaExecutable.Encoding).TrimEnd('\0'));

            dataReader.AlignToWord();

            if (dataReader.PeekWord() == 0)
                dataReader.Position += 2;

            while (entries.Count < (int)Index.Count)
                entries.Add("");
        }

        bool ReadText(IDataReader dataReader)
        {
            // The next section starts with an amount of 300 as a dword.
            // If we find it we stop reading by returning false. For safety
            // we will check if the value is lower than 0x1000 as the offsets
            // here will be over 0x8000.
            var next = dataReader.PeekDword();
            var nextWord = next >> 8;

            if (nextWord > 0x100 && nextWord < 0x1000)
            {
                --dataReader.Position;
                return false;
            }

            nextWord >>= 8;

            if (nextWord > 0x100 && nextWord < 0x1000)
            {
                dataReader.Position -= 2;
                return false;
            }

            nextWord >>= 8;

            if (nextWord > 0x100 && nextWord < 0x1000)
            {
                dataReader.Position -= 3;
                return false;
            }

            if (dataReader.PeekWord() == 0) // offset section / split text with placeholders
            {
                dataReader.AlignToWord();

                if (dataReader.PeekWord() != 0)
                    throw new AmbermoonException(ExceptionScope.Data, "Invalid text section.");

                while (dataReader.PeekDword() == 0)
                    dataReader.Position += 4;

                if (dataReader.PeekByte() != 0)
                    dataReader.Position -= 2;

                string text = "";
                var offsets = new List<uint>();
                int endOffset = dataReader.Position;
                uint firstOffset = uint.MaxValue;

                while (dataReader.PeekByte() == 0 && dataReader.Position < firstOffset)
                {
                    var offset = dataReader.ReadDword();

                    if (offset != 0)
                    {
                        if (offset < firstOffset)
                            firstOffset = offset;

                        offsets.Add(offset);
                    }
                }

                for (int i = 0; i < offsets.Count; ++i)
                {
                    dataReader.Position = (int)offsets[i];
                    text += dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding);

                    if (i != offsets.Count - 1) // Insert placeholder
                        text += "{" + i + "}";

                    if (dataReader.Position > endOffset)
                        endOffset = dataReader.Position;
                }

                entries.Add(text);
                dataReader.Position = endOffset;
            }
            else // just a text
            {
                entries.Add(dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));

                bool sectionFollows = dataReader.PeekDword() == 0;

                while (dataReader.PeekByte() == 0 || dataReader.PeekByte() == 0xff)
                {
                    if (sectionFollows)
                    {
                        if ((dataReader.PeekDword() & 0x0000ffff) > 0xff)
                            break;
                    }
                    else
                        sectionFollows = dataReader.PeekDword() == 0;

                    ++dataReader.Position;
                }
            }

            return true;
        }
    }
}

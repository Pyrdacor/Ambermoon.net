using System;
using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon;

partial class GameCore
{
    bool is3D = false;
    bool swamLastTick = false;
    bool swimDamageHandled = false;
    uint lastSwimDamageHour = 0;
    uint lastSwimDamageMinute = 0;
    readonly List<uint> changedMaps = [];
    bool monstersCanMoveImmediately = false; // this is set when the player just moved so that monsters who see the player can instantly move (2D only)

    public bool Is3D => is3D;
    internal IMapCharacter? CurrentMapCharacter { get; set; } // This is set when interacting with a map character
    public Map? Map => !Ingame ? null : Is3D ? renderMap3D?.Map : renderMap2D?.Map;
    public IMapManager MapManager { get; }
    internal bool MonsterSeesPlayer { get; set; } = false;

    void ShowMap(bool show, bool playMusic = true)
    {
        layout.HideTooltip();

        if (show)
        {
            UpdateUIPalette(true);
            currentBattle = null;
            layout.CancelDrag();
            ResetCursor();
            OpenStorage = null;
            UpdateMapName();
            Resume();
            ResetMoveKeys(true);
            UpdateLight();

            if (playMusic)
            {
                if (lastPlayedSong != null && lastPlayedSong != Song.BarBrawlin)
                    PlayMusic(lastPlayedSong.Value);
                else if (Map!.UseTravelMusic)
                    PlayMusic(TravelType.TravelSong());
                else
                    PlayMusic(Song.Default);
            }
        }
        else
        {
            UpdateUIPalette(false);
            Pause();
        }

        ShowWindowTitle(show);

        if (is3D)
        {
            if (show)
                layout.SetLayout(LayoutType.Map3D, movement.MovementTicks(true, false, TravelType.Walk));
            renderView.GetLayer(Layer.Map3DBackground).Visible = show;
            renderView.GetLayer(Layer.Map3DBackgroundFog).Visible = show;
            renderView.GetLayer(Layer.Map3DCeiling).Visible = show;
            renderView.GetLayer(Layer.Map3D).Visible = show;
            renderView.GetLayer(Layer.Billboards3D).Visible = show;
        }
        else
        {
            if (show)
                layout.SetLayout(LayoutType.Map2D, movement.MovementTicks(false, Map!.UseTravelTypes, TravelType));
            for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
                renderView.GetLayer((Layer)i).Visible = show;
        }

        if (show)
        {
            layout.Reset();
            mapViewRightFillArea = layout.FillArea(new Rect(208, 49, 96, 80), GetUIColor(28), false);
            SetWindow(Window.MapView);

            foreach (var specialItem in EnumHelper.GetValues<SpecialItemPurpose>())
            {
                if (CurrentSavegame!.IsSpecialItemActive(specialItem))
                    layout.AddSpecialItem(specialItem);
            }

            foreach (var activeSpell in EnumHelper.GetValues<ActiveSpellType>())
            {
                if (CurrentSavegame!.ActiveSpells[(int)activeSpell] != null)
                    layout.AddActiveSpell(activeSpell, CurrentSavegame.ActiveSpells[(int)activeSpell], false);
            }
        }
    }

    internal void ResetMapCharacterInteraction(Map map, bool leaveMapCharacter = false)
    {
        if (CurrentMapCharacter != null)
        {
            CurrentMapCharacter.ResetLastInteractionTime();

            if (!leaveMapCharacter)
                CurrentMapCharacter = null;
        }

        if (map.Type == MapType.Map3D)
            RenderMap3D.Reset();
        else
            MapCharacter2D.Reset();
    }

    void Set3DLight(float fade)
    {
        renderView.Set3DFade(fade);
        // TODO: ceiling/floor color
    }

    void Fade3DMapOut(int totalSteps, int timePerStep)
    {
        float div = totalSteps;

        for (int i = 0; i <= totalSteps; i++)
        {
            float light = FadeAlphaToLight(1.0f - i / div);
            AddTimedEvent(TimeSpan.FromMilliseconds(i * timePerStep), () =>
            Set3DLight(light));
        }
    }

    void Fade3DMapIn(int totalSteps, int timePerStep)
    {
        float div = totalSteps;

        for (int i = 0; i <= totalSteps; i++)
        {
            float light = FadeAlphaToLight(i / div);
            AddTimedEvent(TimeSpan.FromMilliseconds(i * timePerStep), () =>
            Set3DLight(light));
        }
    }

    private void ChangeMapAreaExploration(uint mapIndex, uint x, uint y, uint width, uint height,
        RectangularExplorationEvent.ExplorationType explorationType)
    {
        if (width == 0 || height == 0)
            return;

        var map = MapManager.GetMap(mapIndex);

        // we use 0-based coordinates
        x = Math.Max(1, x) - 1;
        y = Math.Max(1, y) - 1;

        if (x >= map.Width || y >= map.Height)
            return;

        var automap = CurrentSavegame!.Automaps.TryGetValue(mapIndex, out var existingAutomap)
            ? existingAutomap
            : new Automap() { ExplorationBits = new byte[(map.Width * map.Height + 7) / 8] };

        uint startX = x;
        uint endX = Math.Min(x + width, (uint)map.Width);
        uint endY = Math.Min(y + height, (uint)map.Height);

        for (; y < endY; y++)
        {
            int rowBaseBit = (int)(y * (uint)map.Width + x);

            for (x = startX; x < endX; x++)
            {
                int bitIndex = rowBaseBit++;
                int byteIndex = bitIndex >> 3;
                byte mask = (byte)(1 << (bitIndex & 7));

                switch (explorationType)
                {
                    case RectangularExplorationEvent.ExplorationType.Reveal:
                        automap.ExplorationBits[byteIndex] |= mask;
                        break;

                    case RectangularExplorationEvent.ExplorationType.Hide:
                        automap.ExplorationBits[byteIndex] &= (byte)~mask;
                        break;

                    case RectangularExplorationEvent.ExplorationType.Invert:
                        automap.ExplorationBits[byteIndex] ^= mask;
                        break;
                }
            }
        }

        CurrentSavegame.Automaps[Map!.Index] = automap;
    }

    public void ExploreMapArea(RectangularExplorationEvent rectangularExplorationEvent)
    {
        if (!Ingame || Map is null || !Is3D)
            return;

        var mapIndex = rectangularExplorationEvent.MapIndex;

        if (mapIndex == 0)
            mapIndex = Map.Index;

        ChangeMapAreaExploration(mapIndex, rectangularExplorationEvent.X, rectangularExplorationEvent.Y,
            rectangularExplorationEvent.Width, rectangularExplorationEvent.Height, rectangularExplorationEvent.Exploration);
    }

    public void ExploreMapArea(VerticalLineRevealEvent verticalLineRevealEvent)
    {
        if (!Ingame || Map is null || !Is3D)
            return;

        var reveal = RectangularExplorationEvent.ExplorationType.Reveal;

        if (verticalLineRevealEvent.Height1 != 0)
        {
            ChangeMapAreaExploration(Map.Index, verticalLineRevealEvent.X1, verticalLineRevealEvent.Y1,
                1u, verticalLineRevealEvent.Height1, reveal);
        }

        if (verticalLineRevealEvent.Height2 != 0)
        {
            ChangeMapAreaExploration(Map.Index, verticalLineRevealEvent.X2, verticalLineRevealEvent.Y2,
                1u, verticalLineRevealEvent.Height2, reveal);
        }

        if (verticalLineRevealEvent.Height3 != 0)
        {
            ChangeMapAreaExploration(Map.Index, verticalLineRevealEvent.X3, verticalLineRevealEvent.Y3,
                1u, verticalLineRevealEvent.Height3, reveal);
        }
    }

    public bool ExploreMap()
    {
        if (!Ingame || Map is null || !Is3D)
            return false;

        if (!CurrentSavegame!.Automaps.TryGetValue(Map.Index, out var automap))
        {
            automap = new Automap { ExplorationBits = Enumerable.Repeat((byte)0xff, (Map.Width * Map.Height + 7) / 8).ToArray() };
            CurrentSavegame.Automaps[Map.Index] = automap;
        }
        else
        {
            automap.ExplorationBits = Enumerable.Repeat((byte)0xff, (Map.Width * Map.Height + 7) / 8).ToArray();
        }

        if (Map.GotoPoints?.Count > 0)
        {
            foreach (var gotoPoint in Map.GotoPoints)
            {
                CurrentSavegame.ActivateGotoPoint(gotoPoint.Index);
            }
        }

        if (currentWindow.Window == Window.Automap && nextClickHandler != null)
        {
            var automapOptions = (AutomapOptions)currentWindow.WindowParameters[0]!;
            var oldCloseWindowHandler = closeWindowHandler;
            closeWindowHandler = backToMap =>
            {
                oldCloseWindowHandler?.Invoke(backToMap);
                ShowAutomap(automapOptions);
            };
            var nextClickHandler = this.nextClickHandler;
            this.nextClickHandler = null;
            nextClickHandler(MouseButtons.Right); // This closes the automap
        }

        return true;
    }

    public bool ActivateTransport(TravelType travelType)
    {
        if (travelType == TravelType.Walk ||
            travelType == TravelType.Swim)
            throw new AmbermoonException(ExceptionScope.Application, "Walking and swimming should not be set via ActivateTransport");

        if (!Map!.UseTravelTypes)
            return false;

        if (TravelType != TravelType.Walk)
            return false;

        void Activate()
        {
            TravelType = travelType;
            layout.TransportEnabled = true;
            if (layout.ButtonGridPage == 1)
            {
                layout.EnableButton(3, true);
                layout.EnableButton(5, travelType.CanCampOn());
            }
        }

        if (WindowActive)
            CloseWindow(Activate);
        else
            Activate();

        return true;
    }

    internal void ToggleTransport()
    {
        uint x = (uint)player!.Position.X;
        uint y = (uint)player.Position.Y;
        var mapIndex = renderMap2D!.GetMapFromTile(x, y).Index;
        var transport = GetTransportAtPlayerLocation(out int? index);

        if (transport == null)
        {
            if (TravelType.UsesMapObject())
            {
                index = null;
                for (int i = 0; i < CurrentSavegame!.TransportLocations.Length; ++i)
                {
                    if (CurrentSavegame.TransportLocations[i] == null)
                    {
                        CurrentSavegame.TransportLocations[i] = new TransportLocation
                        {
                            MapIndex = mapIndex,
                            Position = Map!.IsWorldMap
                                ? new Position((int)x % 50 + 1, (int)y % 50 + 1)
                                : new Position((int)x + 1, (int)y + 1),
                            TravelType = TravelType
                        };
                        index = i;
                        break;
                    }
                }

                if (index != null)
                    renderMap2D.PlaceTransport(mapIndex, Map!.IsWorldMap ? x % 50 : x, Map.IsWorldMap ? y % 50 : y, TravelType, index.Value);
                else
                    return;
            }
            else
            {
                layout.TransportEnabled = false;
                if (layout.ButtonGridPage == 1)
                    layout.EnableButton(3, false);
            }

            var tile = renderMap2D[player.Position];

            if (tile.Type == Map.TileType.Water &&
                (!TravelType.UsesMapObject() ||
                !TravelType.CanStandOn()))
                StartSwimming();
            else
                TravelType = TravelType.Walk;

            if (layout.ButtonGridPage == 1)
                layout.EnableButton(5, TravelType.CanCampOn());

            renderMap2D.TriggerEvents(player2D!, EventTrigger.Move, x, y, MapManager, CurrentTicks, CurrentSavegame!);
        }
        else if (transport != null && TravelType == TravelType.Walk)
        {
            CurrentSavegame!.TransportLocations[index!.Value] = null;
            renderMap2D.RemoveTransport(index.Value);
            ActivateTransport(transport.TravelType);
        }
    }

    TransportLocation? GetTransportAtPlayerLocation(out int? index)
    {
        index = null;
        var mapIndex = renderMap2D!.GetMapFromTile((uint)player!.Position.X, (uint)player.Position.Y).Index;
        // Note: Savegame stores positions 1-based but we 0-based so increase by 1,1 for tests below.
        var position = Map!.IsWorldMap
            ? new Position(player.Position.X % 50 + 1, player.Position.Y % 50 + 1)
            : new Position(player.Position.X + 1, player.Position.Y + 1);

        for (int i = 0; i < CurrentSavegame!.TransportLocations.Length; ++i)
        {
            var transport = CurrentSavegame.TransportLocations[i];

            if (transport != null)
            {
                if (transport.MapIndex == mapIndex && transport.Position == position)
                {
                    index = i;
                    return transport;
                }
            }
        }

        return null;
    }

    List<TransportLocation> GetTransportsInVisibleArea(out TransportLocation? transportAtPlayerIndex)
    {
        transportAtPlayerIndex = null;
        var transports = new List<TransportLocation>();

        if (!Map!.UseTravelTypes)
            return transports;

        var mapIndex = renderMap2D!.GetMapFromTile((uint)player!.Position.X, (uint)player.Position.Y).Index;
        // Note: Savegame stores positions 1-based but we 0-based so increase by 1,1 for tests below.
        var position = Map.IsWorldMap
            ? new Position(player.Position.X % 50 + 1, player.Position.Y % 50 + 1)
            : new Position(player.Position.X + 1, player.Position.Y + 1);

        for (int i = 0; i < CurrentSavegame!.TransportLocations.Length; ++i)
        {
            var transport = CurrentSavegame.TransportLocations[i];

            if (transport != null && renderMap2D.IsMapVisible(transport.MapIndex))
            {
                transports.Add(transport);

                if (transport.MapIndex == mapIndex && transport.Position == position)
                    transportAtPlayerIndex = transport;
            }
        }

        return transports;
    }

    void StartSwimming()
    {
        TravelType = TravelType.Swim;
        DoSwimDamage();
    }

    void DoSwimDamage(uint numTicks = 1, Action<bool>? finishAction = null)
    {
        lastSwimDamageHour = GameTime!.Hour;
        lastSwimDamageMinute = GameTime.Minute;
        swamLastTick = true;

        uint CalculateDamage(PartyMember partyMember)
        {
            var swimSkill = partyMember.Skills[Skill.Swim].TotalCurrentValue;

            if (swimSkill >= 99)
                return 0;

            var factor = (100 - swimSkill) / 2;
            uint hitPoints = partyMember.HitPoints.CurrentValue;
            uint totalDamage = 0;

            for (uint i = 0; i < numTicks; ++i)
            {
                uint damage = Math.Max(1, factor * hitPoints / 100);
                totalDamage += damage;
                hitPoints -= damage;
            }

            return totalDamage;
        }

        // Make sure the party stops moving after someone died
        finishAction ??= someoneDied =>
        {
            if (someoneDied)
            {
                clickMoveActive = false;
                CurrentMobileAction = MobileAction.None;
                ResetMoveKeys(true);
            }
        };

        DamageAllPartyMembers(CalculateDamage, null, null, finishAction);
    }

    void RemoveMapTile(Map map, uint x, uint y, bool save)
    {
        UpdateMapTile(new ChangeTileEvent
        {
            Type = EventType.ChangeTile,
            Index = uint.MaxValue,
            FrontTileIndex = 0,
            MapIndex = map.Index,
            X = x + 1,
            Y = y + 1
        }, null, null, save);
    }

    internal uint GetMapFrontTileIndex(Map? map, uint x, uint y)
    {
        map ??= Map;

        if (map == null)
            return 0;

        if (map.Type == MapType.Map2D)
            return map.Tiles[x, y].FrontTileIndex;

        var tile3D = map.Blocks[x, y];

        if (tile3D.MapBorder)
            return 255;

        if (tile3D.WallIndex != 0)
            return 100 + tile3D.WallIndex;

        return tile3D.ObjectIndex;
    }

    internal void UpdateMapTile(ChangeTileEvent changeTileEvent, uint? currentX = null, uint? currentY = null,
        bool save = true)
    {
        bool sameMap = changeTileEvent.MapIndex == 0 || changeTileEvent.MapIndex == Map!.Index;
        var map = sameMap ? Map! : MapManager.GetMap(changeTileEvent.MapIndex);
        uint x = changeTileEvent.X == 0
            ? (currentX ?? throw new AmbermoonException(ExceptionScope.Data, "No change tile position given"))
            : changeTileEvent.X - 1;
        uint y = changeTileEvent.Y == 0
            ? (currentY ?? throw new AmbermoonException(ExceptionScope.Data, "No change tile position given"))
            : changeTileEvent.Y - 1;

        if (save)
        {
            // Add it to the savegame as well.
            var changeEvents = CurrentSavegame!.TileChangeEvents;

            if (!changeEvents.ContainsKey(map!.Index))
                changeEvents[map.Index] = [changeTileEvent];
            else
            {
                var existing = changeEvents[map.Index].FirstOrDefault(e => e.X == changeTileEvent.X && e.Y == changeTileEvent.Y);

                if (existing != null)
                    changeEvents[map.Index].Remove(existing);

                changeEvents[map.Index].Add(changeTileEvent);
            }
        }

        if (!changedMaps.Contains(map.Index))
            changedMaps.Add(map.Index);

        if (map.Type == MapType.Map3D)
        {
            var block = map.Blocks[x, y];
            block.ObjectIndex = changeTileEvent.ObjectIndex;
            block.WallIndex = changeTileEvent.WallIndex;
            block.MapBorder = false;

            if (sameMap)
                renderMap3D!.UpdateBlock(x, y);
        }
        else // 2D
        {
            map.UpdateTile(x, y, changeTileEvent.FrontTileIndex, MapManager.GetTilesetForMap(map));

            if (renderMap2D!.IsMapVisible(changeTileEvent.MapIndex, ref x, ref y))
                renderMap2D.UpdateTile(x, y);
        }

        if (changeTileEvent.Next == null)
            ResetMapCharacterInteraction(Map!);
    }

    internal void SetMapEventBit(uint mapIndex, uint eventListIndex, bool bit)
    {
        CurrentSavegame!.SetEventBit(mapIndex, eventListIndex, bit);
    }

    internal void SetMapCharacterBit(uint mapIndex, uint characterIndex, bool bit)
    {
        CurrentSavegame!.SetCharacterBit(mapIndex, characterIndex, bit);

        // Note: That might not work for world maps but there are no characters on those maps.
        if (Map!.Index == mapIndex)
        {
            if (Is3D)
            {
                renderMap3D!.UpdateCharacterVisibility(characterIndex);
            }
            else
            {
                renderMap2D!.UpdateCharacterVisibility(characterIndex);
            }
        }
    }

    internal void SetMapCharacterBit(uint characterBit, bool bit)
    {
        var mapIndex = 1 + characterBit / 32;
        var characterIndex = characterBit % 32;

        SetMapCharacterBit(mapIndex, characterIndex, bit);
    }

    private bool IsMapCharacterActive(uint characterBit)
    {
        var mapIndex = 1 + characterBit / 32;
        var characterIndex = characterBit % 32;

        return !CurrentSavegame!.GetCharacterBit(mapIndex, characterIndex);
    }

    internal void UpdateTransportPosition(int index)
    {
        if (!is3D && Map!.UseTravelTypes)
        {
            var transport = CurrentSavegame!.TransportLocations[index];
            renderMap2D!.RemoveTransport(index);
            renderMap2D.PlaceTransport(transport.MapIndex, (uint)transport.Position.X - 1,
                (uint)transport.Position.Y - 1, transport.TravelType, index);
        }
    }

    private void UpdateOutdoorLight(uint minutesPassed, bool lightOff)
    {
        bool lightBuffBurningOut = false;

        if (!CurrentSavegame!.IsSpellActive(ActiveSpellType.Light) && minutesPassed == 5)
        {
            uint lastHour;

            if (GameTime!.Minute == 0) // hour changed
            {
                lastHour = GameTime.Hour == 0 ? 23 : GameTime.Hour - 1;
            }
            else
            {
                lastHour = GameTime.Hour;
            }

            uint expectedLightIntensity = GetDaytimeLightIntensity(lastHour);

            if (lightIntensity > expectedLightIntensity)
                lightBuffBurningOut = true;
        }

        uint newExpectedLightIntensity = GetDaytimeLightIntensity();

        if (lightBuffBurningOut)
            lightIntensity = (uint)Math.Max(newExpectedLightIntensity, (int)lightIntensity - 16);
        else
            lightIntensity = newExpectedLightIntensity;

        UpdateLight(false, false, false, null, lightBuffBurningOut ? lightIntensity : (uint?)null);

        if (lightOff)
            renderMap3D?.SetFog(Map!, MapManager.GetLabdataForMap(Map), lightOff);
    }

    void RenderMap3D_MapChanged(Map map)
    {
        ResetMoveKeys();
        RunSavegameTileChangeEvents(map.Index);
    }

    void RenderMap2D_MapChanged(Map? lastMap, Map[] maps)
    {
        if (lastMap == null || !lastMap.IsWorldMap ||
            !maps[0].IsWorldMap || lastMap.World != maps[0].World)
            ResetMoveKeys();

        foreach (var map in maps)
            RunSavegameTileChangeEvents(map.Index);
    }

    float Get3DLight()
    {
        uint usedLightIntensity;

        if (CurrentPartyMember!.Conditions.HasFlag(Condition.Blind))
            return 0.0f;

        if (Map!.Flags.HasFlag(MapFlags.Outdoor))
        {
            // This is handled by palette color replacement.
            return 1.0f;
        }
        else if (Map.Flags.HasFlag(MapFlags.Indoor))
        {
            // Indoor always use full brightness.
            usedLightIntensity = 255;
        }
        else
        {
            usedLightIntensity = lightIntensity;
        }

        if (usedLightIntensity == 0)
            return 0.0f;
        else if (usedLightIntensity == 255)
            return 1.0f;

        return usedLightIntensity / 255.0f;
    }

    private uint GetDaytimeLightIntensity()
    {
        uint hour = GameTime!.Hour;

        if (GameTime.Minute == 60) // this might happen during a minute tick just before the hours are adjusted
            hour = (hour + 1) % 24;

        return GetDaytimeLightIntensity(hour);
    }

    private static uint GetDaytimeLightIntensity(uint hour)
    {
        // 17:00-18:59: 128
        // 19:00-19:59: 80
        // 20:00-05:59: 32
        // 06:00-06:59: 80
        // 07:00-07:59: 128
        // 08:00-16:59: 255

        if (hour < 6 || hour >= 20)
            return 32;
        else if (hour < 7)
            return 80;
        else if (hour < 8)
            return 128;
        else if (hour < 17)
            return 255;
        else if (hour < 19)
            return 128;
        else if (hour < 20)
            return 80;
        else
            return 32;
    }

    internal void UpdateLight(bool mapChange = false, bool lightActivated = false, bool playerSwitched = false, Map? map = null,
        uint? customOutdoorLightIntensity = null)
    {
        map ??= Map;

        if (map == null)
            return;

        void ChangeLightRadius(int lastRadius, int newRadius)
        {
            var oldMap = map;
            var lightLevel = CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Light);
            const int timePerChange = 75;
            var timeSpan = TimeSpan.FromMilliseconds(timePerChange);

            void ChangeLightRadius()
            {
                if (oldMap != map || // map changed
                    lightLevel != CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Light)) // light buff changed
                    return;

                int diff = newRadius - lastRadius;

                if (diff != 0)
                {
                    int change = mapChange || playerSwitched ? diff : Math.Sign(diff) * Math.Min(Math.Abs(diff), 8);
                    lastRadius += change;
                    fow2D.Radius = (byte)lastRadius;
                    fow2D.Visible = !is3D && lastRadius < 112;

                    if (newRadius - lastRadius != 0)
                        AddTimedEvent(timeSpan, ChangeLightRadius);
                }
            }

            if (mapChange || playerSwitched)
                ChangeLightRadius();
            else
                AddTimedEvent(timeSpan, ChangeLightRadius);
        }

        if (TravelType == TravelType.Fly)
        {
            // Full light
            lightIntensity = 255;
            fow2D!.Visible = false;
        }
        else if (CurrentPartyMember!.Conditions.HasFlag(Condition.Blind))
        {
            lightIntensity = 0;

            if (!Is3D)
            {
                fow2D!.Radius = 0;
                fow2D.Visible = true;
            }
            else
            {
                renderMap3D!.HideSky();
            }
        }
        else if (Map!.Flags.HasFlag(MapFlags.Outdoor))
        {
            // Light is based on daytime and own light sources
            // Each light spell level adds an additional 32.

            if (!Is3D || customOutdoorLightIntensity == null)
            {
                uint lastIntensity = lightIntensity;

                lightIntensity = GetDaytimeLightIntensity();
                lightIntensity = Math.Min(255, lightIntensity + CurrentSavegame!.GetActiveSpellLevel(ActiveSpellType.Light) * 32);

                if (!Is3D && (lastIntensity != lightIntensity || mapChange))
                {
                    var lastRadius = mapChange ? 0 : (int)(lastIntensity >> 1);
                    var newRadius = (int)(lightIntensity >> 1);
                    fow2D!.Visible = lastIntensity < 224;
                    ChangeLightRadius(lastRadius, newRadius);
                }
            }
        }
        else if (Map.Flags.HasFlag(MapFlags.Indoor))
        {
            // Full light
            lightIntensity = 255;
            fow2D!.Visible = false;

            if (Is3D)
                renderMap3D!.HideSky();
        }
        else // Dungeon
        {
            // Otherwise light is based on own light sources only.
            if (lightActivated || mapChange || playerSwitched)
            {
                if (Is3D)
                {
                    if (mapChange && !CurrentSavegame!.IsSpellActive(ActiveSpellType.Light))
                        lightIntensity = 0;
                    else
                    {
                        var lightLevel = CurrentSavegame!.GetActiveSpellLevel(ActiveSpellType.Light);
                        if (lightLevel > 0 || !playerSwitched)
                        {
                            lightIntensity = Math.Min(255, 176 + CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Light) * 32);
                            if (lightLevel == 1)
                                lightIntensity = Math.Min(255, lightIntensity + 16);
                        }
                    }
                }
                else
                {
                    uint lastIntensity = lightIntensity;
                    lightIntensity = CurrentSavegame!.GetActiveSpellLevel(ActiveSpellType.Light) * 32;

                    if (lastIntensity != lightIntensity)
                    {
                        var lastRadius = (int)(lastIntensity >> 1);
                        var newRadius = (int)(lightIntensity >> 1);
                        fow2D!.Visible = lastIntensity < 224;
                        ChangeLightRadius(lastRadius, newRadius);
                    }
                }
            }
            else if (!Is3D && lightIntensity < 224)
            {
                fow2D!.Radius = (byte)(lightIntensity >> 1);
                fow2D.Visible = true;
            }
            if (Is3D)
            {
                fow2D!.Visible = false;
                renderMap3D!.HideSky();
            }
        }

        if (Is3D)
        {
            fow2D!.Visible = false;
            var light3D = Get3DLight();
            renderView.SetLight(light3D);
            uint lightBuffIntensity = Map!.Flags.HasFlag(MapFlags.Outdoor)
                ? (uint)Math.Max(0, (customOutdoorLightIntensity ?? lightIntensity) - (long)GetDaytimeLightIntensity())
                : lightIntensity;

            renderMap3D!.UpdateSky(lightEffectProvider, GameTime!, lightBuffIntensity);
            renderMap3D.SetColorLightFactor(light3D);
            renderMap3D.SetFog(Map, MapManager.GetLabdataForMap(Map));
        }
        else // 2D
        {
            GetTransportsInVisibleArea(out TransportLocation? transportAtPlayerIndex);

            player2D ??= new Player2D(this, renderView.GetLayer(Layer.Characters), player!, renderMap2D!,
                renderView.SpriteFactory, new Position(0, 0), MapManager);
            player2D.BaselineOffset = !CanSee() || transportAtPlayerIndex != null ? MaxBaseLine :
                player!.MovementAbility > PlayerMovementAbility.Swimming ? 32 : 0;
        }
    }

    void OpenMiniMap(Action? finishAction = null)
    {
        CloseWindow(() =>
        {
            Pause();
            var popup = layout.OpenPopup(Map2DViewArea.Position, 11, 9, true, false);
            var contentArea = popup.ContentArea;

            CursorType = CursorType.Sword;
            TrapMouse(contentArea);

            const int numVisibleTilesX = 72; // (11 - 2) * 16 / 2
            const int numVisibleTilesY = 56; // (9 - 2) * 16 / 2
            int displayWidth = Map!.IsWorldMap ? numVisibleTilesX : Math.Min(numVisibleTilesX, Map.Width);
            int displayHeight = Map.IsWorldMap ? numVisibleTilesY : Math.Min(numVisibleTilesY, Map.Height);
            var baseX = popup.ContentArea.Position.X + (numVisibleTilesX - displayWidth); // 1 tile = 2 pixel, half of it is 1, it's actually * 1 here
            var baseY = popup.ContentArea.Position.Y + (numVisibleTilesY - displayHeight); // 1 tile = 2 pixel, half of it is 1, it's actually * 1 here
            var backgroundFill = layout.FillArea(popup.ContentArea, Render.Color.Black, 90);
            var filledAreas = new List<FilledArea>();
            int drawX = baseX;
            int drawY = baseY;

            var rightMap = Map.IsWorldMap ? MapManager.GetMap(Map.RightMapIndex!.Value) : null;
            var downMap = Map.IsWorldMap ? MapManager.GetMap(Map.DownMapIndex!.Value) : null;
            var downRightMap = Map.IsWorldMap ? MapManager.GetMap(Map.DownRightMapIndex!.Value) : null;
            Func<Map, int, int, KeyValuePair<byte, byte?>>? tileColorProvider = null;

            if (is3D)
            {
                var labdata = MapManager.GetLabdataForMap(Map);

                tileColorProvider = (map, x, y) =>
                {
                    // Note: In original this seems bugged. The map border is drawn in different colors depending on savegame and who knows what.
                    // We just skip map border drawing at all by using color index 0 if there is no wall.
                    if (map.Blocks[x, y].WallIndex == 0 || map.Blocks[x, y].WallIndex >= labdata.Walls.Count)
                        return KeyValuePair.Create((byte)0, (byte?)null);
                    else
                        return KeyValuePair.Create(labdata.Walls[(int)map.Blocks[x, y].WallIndex - 1].ColorIndex, (byte?)null);
                };
            }
            else // 2D
            {
                // Possible adjacent maps should use the same tileset so don't bother to provide 4 tilesets here.
                var tileset = MapManager.GetTilesetForMap(Map);

                tileColorProvider = (map, x, y) =>
                {
                    var backTileIndex = map.Tiles[x, y].BackTileIndex;
                    var frontTileIndex = map.Tiles[x, y].FrontTileIndex;
                    byte backColorIndex = tileset.Tiles[backTileIndex - 1].ColorIndex;
                    byte? frontColorIndex = frontTileIndex == 0 ? null : tileset.Tiles[frontTileIndex - 1].ColorIndex;

                    return KeyValuePair.Create(backColorIndex, frontColorIndex);
                };
            }
            void DrawTile(Map map, int x, int y)
            {
                bool visible = popup.ContentArea.Contains(drawX + 1, drawY + 1);
                var tileColors = tileColorProvider(map, x, y);
                var backArea = layout.FillArea(new Rect(drawX, drawY, 2, 2),
                    GetPaletteColor((int)map.PaletteIndex, renderView.GraphicProvider.PaletteIndexFromColorIndex(map, tileColors.Key)), 100);
                filledAreas.Add(backArea);
                backArea.Visible = visible;

                if (tileColors.Value != null)
                {
                    var color = GetPaletteColor((int)map.PaletteIndex, renderView.GraphicProvider.PaletteIndexFromColorIndex(map, tileColors.Value.Value));
                    var upperRightArea = layout.FillArea(new Rect(drawX + 1, drawY, 1, 1), color, 110);
                    var lowerLeftArea = layout.FillArea(new Rect(drawX, drawY + 1, 1, 1), color, 110);

                    filledAreas.Add(upperRightArea);
                    filledAreas.Add(lowerLeftArea);

                    upperRightArea.Visible = visible;
                    lowerLeftArea.Visible = visible;
                }
            }
            for (int y = 0; y < Map.Height; ++y)
            {
                drawX = baseX;

                for (int x = 0; x < Map.Width; ++x)
                {
                    DrawTile(Map, x, y);
                    drawX += 2;
                }

                if (rightMap != null)
                {
                    for (int x = 0; x < rightMap.Width; ++x)
                    {
                        DrawTile(rightMap, x, y);
                        drawX += 2;
                    }
                }

                drawY += 2;
            }
            if (downMap != null)
            {
                for (int y = 0; y < downMap.Height; ++y)
                {
                    drawX = baseX;

                    for (int x = 0; x < downMap.Width; ++x)
                    {
                        DrawTile(downMap, x, y);
                        drawX += 2;
                    }

                    if (downRightMap != null)
                    {
                        for (int x = 0; x < downRightMap.Width; ++x)
                        {
                            DrawTile(downRightMap, x, y);
                            drawX += 2;
                        }
                    }

                    drawY += 2;
                }
            }
            bool closed = false;
            // 16x10 pixels per frame, stored as one image of 16x40 pixels
            // The real position inside each frame has an offset of 7,4
            var positionMarkerGraphicIndex = Graphics.GetUIGraphicIndex(UIGraphic.PlusBlinkAnimation);
            var positionMarker = popup.AddImage(new Rect(baseX + player!.Position.X * 2 - 7, baseY + player.Position.Y * 2 - 4, 16, 10),
                positionMarkerGraphicIndex, Layer.UI, 120, UIPaletteIndex);
            positionMarker.ClipArea = contentArea;
            var positionMarkerBaseTextureOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(positionMarkerGraphicIndex);
            int positionMarkerFrame = 0;
            void AnimatePosition()
            {
                if (!closed)
                {
                    int textureFactor = (int)(positionMarker.Layer?.TextureFactor ?? 1);
                    positionMarker.TextureAtlasOffset = positionMarkerBaseTextureOffset + new Position(0, positionMarkerFrame * textureFactor);
                    positionMarkerFrame = (positionMarkerFrame + 1) % 4; // 4 frames in total
                    AddTimedEvent(TimeSpan.FromMilliseconds(75), AnimatePosition);
                }
            }
            AnimatePosition();
            popup.Closed += () =>
            {
                closed = true;
                positionMarker.Delete();
                backgroundFill.Destroy();
                filledAreas.ForEach(area => area.Destroy());
                UntrapMouse();
                Resume();
                finishAction?.Invoke();
            };
            nextClickHandler = buttons =>
            {
                if (buttons == MouseButtons.Right)
                {
                    ClosePopup();
                    return true;
                }

                return false;
            };
            if (Map.IsWorldMap)
            {
                // Only world maps can be scrolled.
                // We assume that every map has a size of 50x50.
                // Each scrolling will scroll at least 4 tiles.
                const int tilesPerScroll = 4;
                const int maxScrollX = (100 - numVisibleTilesX) / tilesPerScroll; // 7
                const int maxScrollY = (100 - numVisibleTilesY) / tilesPerScroll; // 11
                int scrollOffsetX = 0; // in 4 pixel chunks
                int scrollOffsetY = 0; // in 4 pixel chunks

                void Scroll(int x, int y)
                {
                    int newX = Util.Limit(0, scrollOffsetX + x, maxScrollX);
                    int newY = Util.Limit(0, scrollOffsetY + y, maxScrollY);

                    if (scrollOffsetX != newX || scrollOffsetY != newY)
                    {
                        int diffX = (newX - scrollOffsetX) * tilesPerScroll;
                        int diffY = (newY - scrollOffsetY) * tilesPerScroll;
                        scrollOffsetX = newX;
                        scrollOffsetY = newY;
                        var diff = new Position(diffX, diffY);

                        foreach (var area in filledAreas)
                        {
                            if (area?.Position != null)
                            {
                                area.Position -= diff;
                                area.Visible = contentArea.Contains(area.Position.X + 1, area.Position.Y + 1);
                            }
                        }

                        positionMarker.X -= diffX;
                        positionMarker.Y -= diffY;
                    }
                }

                void CheckScroll()
                {
                    if (!closed)
                    {
                        AddTimedEvent(TimeSpan.FromMilliseconds(50), () =>
                        {
                            if (InputEnable)
                            {
                                var position = renderView.ScreenToGame(GetMousePosition(lastMousePosition));
                                int x = position.X < contentArea.Left + 4 ? -1 : position.X > contentArea.Right - 4 ? 1 : 0;
                                int y = position.Y < contentArea.Top + 4 ? -1 : position.Y > contentArea.Bottom - 4 ? 1 : 0;

                                if (x != 0 || y != 0)
                                    Scroll(x, y);
                            }

                            CheckScroll();
                        });
                    }
                }

                CheckScroll();
            }
        });
    }

    

    internal void ShowAutomap()
    {
        if (!Map!.Flags.HasFlag(MapFlags.Automapper))
        {
            ShowMessagePopup(DataNameProvider.AutomapperNotWorkingHere);
            return;
        }

        bool showAll = CurrentSavegame!.IsSpellActive(ActiveSpellType.MysticMap);

        ShowAutomap(new AutomapOptions
        {
            SecretDoorsVisible = showAll,
            MonstersVisible = showAll,
            PersonsVisible = showAll,
            TrapsVisible = showAll,
            ShowGotoPoints = true
        });
    }

    

    internal void ShowAutomap(AutomapOptions automapOptions, Action? finishAction = null)
    {
        mobileAutomapScroll.X = 0;
        mobileAutomapScroll.Y = 0;

        void Create()
        {
            Fade(() =>
            {
                // Note: Each tile is displayed as 8x8.
                //       The automap type icons are 16x16 but the lower-left 8x8 area is placed on a tile.
                //       The player pin is 16x32 at the lower-left 8x8 is placed on the tile.
                //       Each horizontal map background tile is 16 pixels wide and can contain 2 map tiles/blocks.
                //       Each vertical map background tile is 32 pixels height and can contain 4 map tiles/blocks.
                //       Fill inner map area with AA7744 (index 6). Lines (like walls) are drawn with 663300 (index 7).
                byte paletteIndex = (byte)(renderView.GraphicProvider.AutomapPaletteIndex - 1);
                var backgroundColor = GetPaletteColor(renderView.GraphicProvider.AutomapPaletteIndex, 6);
                var foregroundColor = GetPaletteColor(renderView.GraphicProvider.AutomapPaletteIndex, 7);
                var labdata = MapManager.GetLabdataForMap(Map);
                int legendPage = 0;
                ILayerSprite[] legendSprites = new ILayerSprite[8];
                UIText[] legendTexts = new UIText[8];
                var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.UI);
                int scrollOffsetX = 0; // in 16 pixel chunks
                int scrollOffsetY = 0; // in 16 pixel chunks

                InputEnable = true;
                ShowMap(false);
                SetWindow(Window.Automap, automapOptions);
                layout.Reset();
                layout.SetLayout(LayoutType.Automap);
                ResetMoveKeys(true);
                CursorType = CursorType.Sword;

                var sprites = new List<ISprite>();
                var animatedSprites = new List<IAnimatedLayerSprite>();
                // key = tile index, value = tileX, tileY, drawX, drawY, boolean -> true = normal blocking wall, false = fake wall, null = count as wall but has automap graphic on it
                var walls = new Dictionary<int, AutomapWall>();
                var gotoPoints = new List<KeyValuePair<Map.GotoPoint, Tooltip>>();
                var automapIcons = new Dictionary<int, ISprite>();
                bool animationsPaused = false;

                #region Legend
                layout.FillArea(new Rect(208, 37, Global.VirtualScreenWidth - 208, Global.VirtualScreenHeight - 37), Render.Color.Black, 9);
                // Legend panels
                var headerArea = new Rect(217, 46, 86, 8);
                layout.AddPanel(headerArea, 11);
                layout.AddText(headerArea.CreateModified(0, 1, 0, -1), DataNameProvider.LegendHeader, TextColor.White, TextAlign.Center, 15);
                var legendArea = new Rect(217, 56, 86, 108);
                layout.AddPanel(legendArea, 11);
                for (int i = 0; i < 8; ++i)
                {
                    legendSprites[i] = layout.AddSprite(new Rect(legendArea.X + 2, legendArea.Y + 4 + i * 13 + Global.GlyphLineHeight - 16, 16, 16),
                        0u, paletteIndex, (byte)(15 + i));
                    legendTexts[i] = layout.AddText(new Rect(legendArea.X + 18, legendArea.Y + 4 + i * 13, 68, Global.GlyphLineHeight), "",
                        TextColor.White, TextAlign.Left, 15);
                }
                void ShowLegendPage(int page)
                {
                    legendPage = page;

                    AddTimedEvent(TimeSpan.FromSeconds(4), ToggleLegendPage);

                    void SetLegendEntry(int index, AutomapType? automapType)
                    {
                        if (automapType == null)
                        {
                            legendSprites[index].Visible = false;
                            legendTexts[index].Visible = false;
                        }
                        else
                        {
                            legendSprites[index].TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetAutomapGraphicIndex(automapType.Value.ToGraphic()!.Value));
                            legendTexts[index].SetText(renderView.TextProcessor.CreateText(DataNameProvider.GetAutomapName(automapType.Value)));
                            legendSprites[index].Visible = true;
                            legendTexts[index].Visible = true;
                        }
                    }

                    if (page == 0)
                    {
                        SetLegendEntry(0, AutomapType.Riddlemouth);
                        SetLegendEntry(1, AutomapType.Teleporter);
                        SetLegendEntry(2, AutomapType.Door);
                        SetLegendEntry(3, AutomapType.Chest);
                        if (automapOptions.TrapsVisible)
                        {
                            SetLegendEntry(4, AutomapType.Spinner);
                            SetLegendEntry(5, AutomapType.Merchant);
                            SetLegendEntry(6, AutomapType.Tavern);
                            SetLegendEntry(7, AutomapType.Special);
                        }
                        else
                        {
                            SetLegendEntry(4, AutomapType.Merchant);
                            SetLegendEntry(5, AutomapType.Tavern);
                            SetLegendEntry(6, AutomapType.Special);
                            SetLegendEntry(7, null);
                        }
                    }
                    else
                    {
                        SetLegendEntry(0, AutomapType.Exit);
                        SetLegendEntry(1, AutomapType.Pile);
                        int index = 2;
                        if (automapOptions.TrapsVisible)
                        {
                            SetLegendEntry(2, AutomapType.Trap);
                            SetLegendEntry(3, AutomapType.Trapdoor);
                            index = 4;
                        }
                        if (automapOptions.MonstersVisible)
                        {
                            SetLegendEntry(index++, AutomapType.Monster);
                        }
                        if (automapOptions.PersonsVisible)
                        {
                            SetLegendEntry(index++, AutomapType.Person);
                        }
                        SetLegendEntry(index++, AutomapType.GotoPoint);
                        while (index < 8)
                            SetLegendEntry(index++, null);
                    }
                }
                void ToggleLegendPage()
                {
                    if (CurrentWindow.Window == Window.Automap)
                        ShowLegendPage(1 - legendPage);
                }
                ShowLegendPage(0);
                var locationArea = new Rect(217, 166, 86, 22);
                layout.AddPanel(locationArea, 11);
                if (CurrentSavegame!.IsSpecialItemActive(SpecialItemPurpose.MapLocation))
                {
                    layout.AddText(new Rect(locationArea.X + 2, locationArea.Y + 3, 70, Global.GlyphLineHeight), DataNameProvider.Location, TextColor.White, TextAlign.Left, 15);
                    layout.AddText(new Rect(locationArea.X + 2, locationArea.Y + 12, 70, Global.GlyphLineHeight), $"X:{player3D!.Position.X + 1,-2} Y:{player3D.Position.Y + 1}", TextColor.White, TextAlign.Left, 15);
                }
                DrawPin(locationArea.Right - 16, locationArea.Bottom - 32, 16, 16, false);
                #endregion

                #region Map
                var automap = CurrentSavegame.Automaps.TryGetValue(Map!.Index, out var a) ? a : null;

                void DrawPin(int x, int y, byte upperDisplayLayer, byte lowerDisplayLayer, bool onMap)
                {
                    var pinHead = !CurrentSavegame.IsSpecialItemActive(SpecialItemPurpose.Compass)
                        ? AutomapGraphic.PinUpperHalf
                        : AutomapGraphic.PinDirectionUp + (int)player3D!.PreciseDirection;
                    var upperSprite = layout.AddSprite(new Rect(x, y, 16, 16), Graphics.GetAutomapGraphicIndex(pinHead), paletteIndex, upperDisplayLayer);
                    var lowerSprite = layout.AddSprite(new Rect(x, y + 16, 16, 16), Graphics.GetAutomapGraphicIndex(AutomapGraphic.PinLowerHalf), paletteIndex, lowerDisplayLayer);

                    if (onMap)
                    {
                        upperSprite.ClipArea = Global.AutomapArea;
                        lowerSprite.ClipArea = Global.AutomapArea;
                        sprites.Add(upperSprite);
                        sprites.Add(lowerSprite);
                    }
                }
                var displayLayers = new Dictionary<int, byte>();
                displayLayers[RenderPlayer.Position.X + RenderPlayer.Position.Y * Map.Width] = 100;
                ILayerSprite AddGraphic(int x, int y, AutomapGraphic automapGraphic, int width, int height, byte displayLayer = 2)
                {
                    ILayerSprite sprite;

                    switch (automapGraphic)
                    {
                        case AutomapGraphic.Riddlemouth:
                        case AutomapGraphic.Teleport:
                        case AutomapGraphic.Spinner:
                        case AutomapGraphic.Trap:
                        case AutomapGraphic.TrapDoor:
                        case AutomapGraphic.Special:
                        case AutomapGraphic.Monster: // this and all above have 4 frames
                        case AutomapGraphic.GotoPoint: // this has 8 frames
                        {
                            var animatedSprite = layout.AddAnimatedSprite(new Rect(x, y, width, height), Graphics.GetAutomapGraphicIndex(automapGraphic),
                                paletteIndex, automapGraphic == AutomapGraphic.GotoPoint ? 8u : 4u, displayLayer);
                            animatedSprites.Add(animatedSprite);
                            sprite = animatedSprite;
                            break;
                        }
                        default:
                            sprite = layout.AddSprite(new Rect(x, y, width, height), Graphics.GetAutomapGraphicIndex(automapGraphic), paletteIndex, displayLayer);
                            break;
                    }

                    sprite.ClipArea = Global.AutomapArea;
                    sprites.Add(sprite);
                    return sprite;
                }
                void AddAutomapType(int tx, int ty, int x, int y, AutomapType automapType,
                    byte displayLayer = 5) // 5: above walls, fake wall overlays and player pin lower half (2, 3 and 4)
                {
                    if (!automapOptions.TrapsVisible && (automapType == AutomapType.Trap ||
                        automapType == AutomapType.Trapdoor || automapType == AutomapType.Spinner))
                        return;

                    byte baseDisplayLayer = displayLayer;
                    var graphic = automapType.ToGraphic();

                    if (graphic != null)
                    {
                        if (tx > 0)
                        {
                            if (displayLayers.ContainsKey(tx - 1 + ty * Map.Width))
                                displayLayer = (byte)Math.Min(255, displayLayers[tx - 1 + ty * Map.Width] + 1);
                            else if (ty > 0)
                            {
                                if (tx < Map.Width - 1 && displayLayers.ContainsKey(tx + 1 + (ty - 1) * Map.Width))
                                    displayLayer = (byte)Math.Min(255, displayLayers[tx + 1 + (ty - 1) * Map.Width] + 1);
                                else if (displayLayers.ContainsKey(tx + (ty - 1) * Map.Width))
                                    displayLayer = (byte)Math.Min(255, displayLayers[tx + (ty - 1) * Map.Width] + 1);
                                else if (tx > 0 && displayLayers.ContainsKey(tx - 1 + (ty - 1) * Map.Width))
                                    displayLayer = (byte)Math.Min(255, displayLayers[tx - 1 + (ty - 1) * Map.Width] + 1);
                            }
                        }
                        else if (ty > 0)
                        {
                            if (tx < Map.Width - 1 && displayLayers.ContainsKey(tx + 1 + (ty - 1) * Map.Width))
                                displayLayer = (byte)Math.Min(255, displayLayers[tx + 1 + (ty - 1) * Map.Width] + 1);
                            else if (displayLayers.ContainsKey(tx + (ty - 1) * Map.Width))
                                displayLayer = (byte)Math.Min(255, displayLayers[tx + (ty - 1) * Map.Width] + 1);
                        }

                        int tileIndex = tx + ty * Map.Width;

                        if (automapIcons.ContainsKey(tileIndex))
                        {
                            // Already an automap icon there -> remove it
                            automapIcons[tileIndex]?.Delete();
                        }

                        automapIcons[tileIndex] = AddGraphic(x, y - 8, graphic.Value, 16, 16, displayLayer);
                        if (!displayLayers.ContainsKey(tileIndex) || displayLayers[tileIndex] < displayLayer)
                            displayLayers[tileIndex] = displayLayer;
                    }
                }
                void AddTile(int tx, int ty, int x, int y)
                {
                    renderMap3D!.CharacterTypeFromBlock((uint)tx, (uint)ty, out var automapType);

                    if (automapType == AutomapType.None)
                        automapType = renderMap3D.AutomapTypeFromBlock((uint)tx, (uint)ty);

                    if (automapType == AutomapType.Monster)
                    {
                        if (automapOptions.MonstersVisible)
                            AddAutomapType(tx, ty, x, y, AutomapType.Monster, 6);
                    }
                    else if (automapType == AutomapType.Person)
                    {
                        if (automapOptions.PersonsVisible)
                            AddAutomapType(tx, ty, x, y, AutomapType.Person, 6);
                    }

                    if (automap != null && !automap.IsBlockExplored(Map, (uint)tx, (uint)ty))
                        return;

                    // Note: Maps are always 3D
                    var block = Map.Blocks[tx, ty];

                    if (block.MapBorder)
                    {
                        // draw nothing
                        return;
                    }
                    if (automapOptions.ShowGotoPoints)
                    {
                        var gotoPoint = Map.GotoPoints.FirstOrDefault(p => p.X == tx + 1 && p.Y == ty + 1); // positions of goto points are 1-based
                        if (gotoPoint != null && CurrentSavegame.IsGotoPointActive(gotoPoint.Index))
                        {
                            AddAutomapType(tx, ty, x, y, AutomapType.GotoPoint);
                            gotoPoints.Add(KeyValuePair.Create(gotoPoint,
                                layout.AddTooltip(new Rect(x, y, 8, 8), gotoPoint.Name, TextColor.White)));
                        }
                    }
                    if (automapType != AutomapType.None && automapType != AutomapType.Monster && automapType != AutomapType.Person)
                        AddAutomapType(tx, ty, x, y, automapType);
                    if (block.WallIndex != 0)
                    {
                        var wall = labdata.Walls[(int)block.WallIndex - 1];
                        bool blockingWall = block.BlocksPlayer(labdata);

                        // Walls that don't block and use transparency are not considered walls
                        // nor fake walls. For example a destroyed cobweb uses this.
                        // Fake walls on the other hand won't block but are not transparent.
                        if (wall.AutomapType == AutomapType.Wall || blockingWall || !wall.Flags.HasFlag(Tileset.TileFlags.Transparency))
                        {
                            bool draw = automapType == AutomapType.None || wall.AutomapType == AutomapType.Wall ||
                                automapType == AutomapType.Tavern || automapType == AutomapType.Merchant || automapType == AutomapType.Door;

                            walls.Add(tx + ty * Map.Width, new AutomapWall
                            {
                                TileX = tx,
                                TileY = ty,
                                DrawX = x,
                                DrawY = y,
                                NormalWall = draw ? blockingWall : (bool?)null,
                                BlocksSight = wall.Flags.HasFlag(Tileset.TileFlags.BlockSight)
                            });
                        }
                    }
                }

                int x = Global.AutomapArea.X;
                int y = Global.AutomapArea.Y;
                int xParts = (Map.Width + 1) / 2;
                int yParts = (Map.Height + 3) / 4;
                var totalArea = new Rect(Global.AutomapArea.X, Global.AutomapArea.Y, 64 + xParts * 16, 64 + yParts * 32);
                var mapNameBounds = new Rect(Global.AutomapArea.X, Global.AutomapArea.Y + 32, totalArea.Width, Global.GlyphLineHeight);
                var mapName = layout.AddText(mapNameBounds, Map.Name, TextColor.White, TextAlign.Center, 3);

                // Fill background black
                layout.FillArea(Global.AutomapArea, Render.Color.Black, 0);

                #region Upper border
                AddGraphic(x, y, AutomapGraphic.MapUpperLeft, 32, 32);
                x += 32;
                for (int tx = 0; tx < xParts; ++tx)
                {
                    AddGraphic(x, y, AutomapGraphic.MapBorderTop1 + tx % 4, 16, 32);
                    x += 16;
                }
                AddGraphic(x, y, AutomapGraphic.MapUpperRight, 32, 32);
                x = Global.AutomapArea.X;
                y += 32;
                #endregion

                #region Map content
                FilledArea? mapFill = null;

                void FillMap()
                {
                    mapFill?.Destroy();
                    var fillArea = new Rect(Global.AutomapArea.X + 32 - scrollOffsetX * 16, Global.AutomapArea.Y + 32 - scrollOffsetY * 16, xParts * 16, yParts * 32);
                    var clipArea = new Rect(Global.AutomapArea);
                    int maxScrollX = (totalArea.Width - 208) / 16;
                    int maxScrollY = (totalArea.Height - 160) / 16;
                    if (scrollOffsetX >= maxScrollX - 1)
                        clipArea = clipArea.SetWidth(clipArea.Width - (2 - (maxScrollX - scrollOffsetX)) * 16);
                    if (scrollOffsetY >= maxScrollY - 1)
                        clipArea = clipArea.SetHeight(clipArea.Height - (2 - (maxScrollY - scrollOffsetY)) * 16);
                    fillArea.Clip(clipArea);
                    mapFill = layout.FillArea(fillArea, backgroundColor, 1);
                }
                FillMap();
                for (int ty = 0; ty < Map.Height; ++ty)
                {
                    if (ty % 4 == 0)
                    {
                        AddGraphic(Global.AutomapArea.X, y, AutomapGraphic.MapBorderLeft1 + (ty % 8) / 4, 32, 32);
                    }

                    x = Global.AutomapArea.X + 32;

                    for (int tx = 0; tx < Map.Width; ++tx)
                    {
                        AddTile(tx, ty, x, y);
                        x += 8;
                    }

                    if (ty % 4 == 0)
                    {
                        if (Map.Width % 2 != 0)
                            x += 8;
                        AddGraphic(x, y, AutomapGraphic.MapBorderRight1 + (ty % 8) / 4, 32, 32);
                    }

                    y += 8;
                }
                // Draw walls
                foreach (var wall in walls)
                {
                    int tx = wall.Value.TileX;
                    int ty = wall.Value.TileY;
                    int dx = wall.Value.DrawX;
                    int dy = wall.Value.DrawY;
                    bool? type = wall.Value.NormalWall;
                    bool blocksSight = wall.Value.BlocksSight;

                    if (type != null)
                    {
                        bool ContainsSameWall(int x, int y, out bool otherWall)
                        {
                            otherWall = false;

                            if (!walls.TryGetValue(x + y * Map.Width, out var wall))
                                return false;

                            // Note: This is used to detect if walls should be
                            // merged visually. There are some special walls that
                            // have a different block sight state (e.g. the crystal
                            // wall in the temple of brotherhood).
                            // Those should be treated as "another" wall so we will
                            // return false here if the block sight states do not match.
                            otherWall = blocksSight != wall.BlocksSight;

                            return !otherWall || wall.NormalWall == null;
                        }

                        bool hasOtherWallLeft = false;
                        bool hasOtherWallUp = false;
                        bool hasOtherWallRight = false;
                        bool hasOtherWallDown = false;
                        bool hasWallLeft = tx > 0 && ContainsSameWall(tx - 1, ty, out hasOtherWallLeft);
                        bool hasWallUp = ty > 0 && ContainsSameWall(tx, ty - 1, out hasOtherWallUp);
                        bool hasWallRight = tx < Map.Width - 1 && ContainsSameWall(tx + 1, ty, out hasOtherWallRight);
                        bool hasWallDown = ty < Map.Height - 1 && ContainsSameWall(tx, ty + 1, out hasOtherWallDown);
                        int wallGraphicType = 15; // closed

                        if (hasWallLeft)
                        {
                            if (hasWallRight)
                            {
                                if (hasWallUp)
                                {
                                    if (hasWallDown)
                                    {
                                        // all directions open (+ crossing)
                                        wallGraphicType = 12;
                                    }
                                    else
                                    {
                                        // left, right and top open (T crossing)
                                        wallGraphicType = 8;
                                    }
                                }
                                else if (hasWallDown)
                                {
                                    // left, right and bottom open (T crossing)
                                    wallGraphicType = 10;
                                }
                                else
                                {
                                    // left and right open
                                    wallGraphicType = 14;
                                }
                            }
                            else
                            {
                                if (hasWallUp)
                                {
                                    if (hasWallDown)
                                    {
                                        // left, top and bottom open (T crossing)
                                        wallGraphicType = 11;
                                    }
                                    else
                                    {
                                        // left and top open (corner)
                                        wallGraphicType = 7;
                                    }
                                }
                                else if (hasWallDown)
                                {
                                    // left and bottom open (corner)
                                    wallGraphicType = 5;
                                }
                                else
                                {
                                    // only left open
                                    wallGraphicType = 3;
                                }
                            }
                        }
                        else if (hasWallRight)
                        {
                            if (hasWallUp)
                            {
                                if (hasWallDown)
                                {
                                    // right, top and bottom open (T crossing)
                                    wallGraphicType = 9;
                                }
                                else
                                {
                                    // right and top open
                                    wallGraphicType = 6;
                                }
                            }
                            else if (hasWallDown)
                            {
                                // right and bottom open (corner)
                                wallGraphicType = 4;
                            }
                            else
                            {
                                // only right open
                                wallGraphicType = 1;
                            }
                        }
                        else
                        {
                            if (hasWallUp)
                            {
                                if (hasWallDown)
                                {
                                    // top and bottom open
                                    wallGraphicType = 13;
                                }
                                else
                                {
                                    // only top open
                                    wallGraphicType = 0;
                                }
                            }
                            else if (hasWallDown)
                            {
                                // only bottom open
                                wallGraphicType = 2;
                            }
                            else
                            {
                                if (hasOtherWallLeft || hasOtherWallRight)
                                {
                                    // left and right open
                                    wallGraphicType = 14;
                                }
                                else if (hasOtherWallUp || hasOtherWallDown)
                                {
                                    // top and bottom open
                                    wallGraphicType = 13;
                                }
                                else
                                {
                                    // closed single wall
                                    wallGraphicType = 15;
                                }
                            }
                        }

                        var sprite = layout.AddSprite(new Rect(dx, dy, 8, 8), Graphics.GetCustomUIGraphicIndex(UICustomGraphic.AutomapWallFrames), paletteIndex, 2);
                        int textureFactor = (int)renderView.GetLayer(Layer.UI).TextureFactor;
                        sprite.TextureAtlasOffset = new Position(sprite.TextureAtlasOffset.X + wallGraphicType * 8 * textureFactor, sprite.TextureAtlasOffset.Y);
                        sprite.ClipArea = Global.AutomapArea;
                        sprites.Add(sprite);

                        if (type == false && automapOptions.SecretDoorsVisible) // fake wall
                        {
                            sprite = layout.AddSprite(new Rect(dx, dy, 8, 8), Graphics.GetCustomUIGraphicIndex(UICustomGraphic.FakeWallOverlay), paletteIndex, 3);
                            sprite.ClipArea = Global.AutomapArea;
                            sprites.Add(sprite);
                        }
                    }
                }
                // Animate automap icons
                void Animate()
                {
                    if (CurrentWindow.Window == Window.Automap && !animationsPaused)
                    {
                        foreach (var animatedSprite in animatedSprites)
                            ++animatedSprite.CurrentFrame;

                        AddTimedEvent(TimeSpan.FromMilliseconds(120), Animate);
                    }
                }
                Animate();
                // Draw player pin
                DrawPin(Global.AutomapArea.X + 32 + RenderPlayer.Position.X * 8, Global.AutomapArea.Y + 32 + RenderPlayer.Position.Y * 8 - 24, 100, 100, true);
                #endregion

                #region Lower border
                x = Global.AutomapArea.X;
                while ((y - Global.AutomapArea.Y) % 32 != 0)
                    y += 8;
                AddGraphic(x, y, AutomapGraphic.MapLowerLeft, 32, 32);
                x += 32;
                for (int tx = 0; tx < xParts; ++tx)
                {
                    AddGraphic(x, y, AutomapGraphic.MapBorderBottom1 + tx % 4, 16, 32);
                    x += 16;
                }
                AddGraphic(x, y, AutomapGraphic.MapLowerRight, 32, 32);
                #endregion

                void Scroll(int x, int y)
                {
                    // The automap screen is 208x163 but we use 208x160 so they are both dividable by 16.
                    // If scrolled to the left there is the 32 pixel wide border so you can see max 22 tiles (208 - 32) / 8 = 22.
                    // Scrolling right is possible unless the 32 pixel wide border on the right is fully visible.
                    // The total automap width is 64 + xParts * 16. So max scroll offset X in tiles is (64 + xParts * 16 - 208) / 16.
                    // We will always scroll by 2 tiles (16 pixel chunks) in both directions.

                    int maxScrollX = (totalArea.Width - 208) / 16;
                    int maxScrollY = (totalArea.Height - 160) / 16;
                    int newX = Util.Limit(0, scrollOffsetX + x, maxScrollX);
                    int newY = Util.Limit(0, scrollOffsetY + y, maxScrollY);

                    if (scrollOffsetX != newX || scrollOffsetY != newY)
                    {
                        int diffX = (newX - scrollOffsetX) * 16;
                        int diffY = (newY - scrollOffsetY) * 16;
                        scrollOffsetX = newX;
                        scrollOffsetY = newY;

                        mapName.SetBounds(mapNameBounds.CreateOffset(-newX * 16, -newY * 16));
                        mapName.Clip(Global.AutomapArea);
                        FillMap();

                        foreach (var sprite in sprites)
                        {
                            sprite.X -= diffX;
                            sprite.Y -= diffY;
                        }

                        foreach (var gotoPoint in gotoPoints)
                        {
                            gotoPoint.Value.Area!.Position.X -= diffX;
                            gotoPoint.Value.Area.Position.Y -= diffY;
                        }

                        // Update active tooltips
                        CursorType cursorType = CursorType.None;
                        layout.Hover(GetMousePosition(lastMousePosition), ref cursorType);
                    }
                }

                void SetupClickHandlers()
                {
                    nextClickHandler = buttons =>
                    {
                        if (buttons == MouseButtons.Right)
                        {
                            Exit();
                            return true;
                        }
                        else if (buttons == MouseButtons.Left && gotoPoints.Count != 0)
                        {
                            var mousePosition = renderView.ScreenToGame(GetMousePosition(lastMousePosition));

                            var clickedGotoPoint = gotoPoints.FirstOrDefault(gotoPoint => gotoPoint.Value.Area!.Contains(mousePosition));

                            // Be a bit more forgiving on mobile devices if they not exactly hit the small circle
                            if (clickedGotoPoint.Key == null && CoreConfiguration.IsMobile)
                                clickedGotoPoint = gotoPoints.FirstOrDefault(gotoPoint => gotoPoint.Value.Area!.CreateModified(-6, -6, 12, 12).Contains(mousePosition));

                            if (clickedGotoPoint.Key != null)
                            {
                                void AbortGoto()
                                {
                                    animationsPaused = false;
                                    Animate();
                                    TrapMouse(Global.AutomapArea);
                                    SetupClickHandlers();
                                }

                                layout.HideTooltip();
                                UntrapMouse();
                                animationsPaused = true;
                                if (!CanSee())
                                {
                                    ShowMessagePopup(DataNameProvider.DarkDontFindWayBack, AbortGoto, TextAlign.Left, 202);
                                }
                                else if (MonsterSeesPlayer)
                                {
                                    ShowMessagePopup(DataNameProvider.WayBackTooDangerous, AbortGoto, TextAlign.Left, 202);
                                }
                                else
                                {
                                    ShowDecisionPopup(DataNameProvider.ReallyWantToGoThere, response =>
                                    {
                                        if (response == PopupTextEvent.Response.Yes)
                                        {
                                            if (player3D!.Position.X + 1 == clickedGotoPoint.Key.X && player3D.Position.Y + 1 == clickedGotoPoint.Key.Y)
                                            {
                                                ShowMessagePopup(DataNameProvider.AlreadyAtGotoPoint, AbortGoto, TextAlign.Center, 202);
                                            }
                                            else
                                            {
                                                Exit(() =>
                                                {
                                                    var xDiff = Math.Abs((int)clickedGotoPoint.Key.X - (player3D.Position.X + 1));
                                                    var yDiff = Math.Abs((int)clickedGotoPoint.Key.Y - (player3D.Position.Y + 1));
                                                    uint ticks = (uint)Util.Round((xDiff + yDiff) * 0.2f);
                                                    GameTime!.Ticks(ticks);
                                                    Teleport(Map.Index, clickedGotoPoint.Key.X, clickedGotoPoint.Key.Y, clickedGotoPoint.Key.Direction, out _, true);
                                                });
                                            }
                                        }
                                        else
                                        {
                                            AbortGoto();
                                        }
                                    }, 1, 202, TextAlign.Center);
                                }
                                return true;
                            }
                        }

                        return false;
                    };
                }
                SetupClickHandlers();

                #endregion

                bool closed = false;

                void Exit(Action? followAction = null)
                {
                    var exitAction = finishAction == null ? followAction : () =>
                    {
                        followAction?.Invoke();
                        finishAction();
                    };

                    closed = true;
                    UntrapMouse();
                    if (currentWindow.Window == Window.Automap)
                        CloseWindow(exitAction);
                    else
                        exitAction?.Invoke();
                }

                void CheckScroll()
                {
                    if (!closed)
                    {
                        AddTimedEvent(TimeSpan.FromMilliseconds(100), () =>
                        {
                            if (InputEnable)
                            {
                                if (CoreConfiguration.IsMobile)
                                {
                                    int x = Util.Round(mobileAutomapScroll.X);
                                    int y = Util.Round(mobileAutomapScroll.Y);

                                    if (x != 0 || y != 0)
                                    {
                                        if (x != 0)
                                            mobileAutomapScroll.X -= x;
                                        if (y != 0)
                                            mobileAutomapScroll.Y -= y;
                                        Scroll(x, y);
                                    }
                                }
                                else
                                {
                                    var position = renderView.ScreenToGame(GetMousePosition(lastMousePosition));
                                    int x = position.X < 4 ? -1 : position.X > 204 ? 1 : 0;
                                    int y = position.Y < 41 ? -1 : position.Y > 196 ? 1 : 0;

                                    if (x != 0 || y != 0)
                                        Scroll(x, y);
                                    else
                                    {
                                        bool left = keys[(int)Key.Left] || keys[(int)Key.A] || keys[(int)Key.Home];
                                        bool right = keys[(int)Key.Right] || keys[(int)Key.D] || keys[(int)Key.End];
                                        bool up = keys[(int)Key.Up] || keys[(int)Key.W] || keys[(int)Key.PageUp];
                                        bool down = keys[(int)Key.Down] || keys[(int)Key.S] || keys[(int)Key.PageDown];

                                        if (left && !right)
                                            x = -1;
                                        else if (right && !left)
                                            x = 1;
                                        if (up && !down)
                                            y = -1;
                                        else if (down && !up)
                                            y = 1;

                                        if (x != 0 || y != 0)
                                            Scroll(x, y);
                                    }
                                }
                            }

                            CheckScroll();
                        });
                    }
                }

                CheckScroll();

                // Initial scroll
                int startScrollX = Math.Max(0, (player!.Position.X - 6) / 2);
                int startScrollY = Math.Max(0, (player.Position.Y - 8) / 2);
                Scroll(startScrollX, startScrollY);

                lastMousePosition = renderView.GameToScreen(Global.AutomapArea.Center);
                TrapMouse(Global.AutomapArea);
                UpdateCursor();
            });
        }

        if (currentWindow.Window == Window.Automap)
            Create();
        else
            CloseWindow(Create);
    }

    internal void ShowRiddlemouth(Map map, RiddlemouthEvent riddlemouthEvent, Action solvedHandler, bool showRiddle = true)
    {
        Fade(() =>
        {
            SetWindow(Window.Riddlemouth, riddlemouthEvent, solvedHandler);
            layout.SetLayout(LayoutType.Riddlemouth);
            ShowMap(false);
            layout.Reset();
            var riddleArea = new Rect(16, 50, 176, 144);
            layout.FillArea(riddleArea, GetUIColor(28), false);
            var riddleText = ProcessText(map.GetText((int)riddlemouthEvent.RiddleTextIndex, DataNameProvider.TextBlockMissing));
            var solutionResponseText = ProcessText(map.GetText((int)riddlemouthEvent.SolutionTextIndex, DataNameProvider.TextBlockMissing));
            void ShowRiddle()
            {
                InputEnable = false;
                HeadSpeak();
                layout.OpenTextPopup(riddleText, riddleArea.Position, riddleArea.Width, riddleArea.Height, true, true, true, TextColor.White).Closed += () =>
                {
                    InputEnable = true;
                };
            }
            void TestSolution(string solution)
            {
                if (string.Compare(textDictionary.Entries[(int)riddlemouthEvent.CorrectAnswerDictionaryIndex1], solution, true) == 0 ||
                    (riddlemouthEvent.CorrectAnswerDictionaryIndex1 != riddlemouthEvent.CorrectAnswerDictionaryIndex2 &&
                        string.Compare(textDictionary.Entries[(int)riddlemouthEvent.CorrectAnswerDictionaryIndex2], solution, true) == 0))
                {
                    InputEnable = false;
                    HeadSpeak();
                    layout.OpenTextPopup(solutionResponseText, riddleArea.Position, riddleArea.Width, riddleArea.Height, true, true, true, TextColor.White, () =>
                    {
                        Exit(() =>
                        {
                            InputEnable = true;
                            solvedHandler?.Invoke();
                        });
                    });
                }
                else
                {
                    if (!textDictionary.Entries.Any(entry => string.Compare(entry, solution, true) == 0))
                        solution = DataNameProvider.That;
                    var failedText = ProcessText(solution + DataNameProvider.WrongRiddlemouthSolutionText);
                    InputEnable = false;
                    HeadSpeak();
                    layout.OpenTextPopup(failedText, riddleArea.Position, riddleArea.Width, riddleArea.Height, true, true, true, TextColor.White).Closed += () =>
                    {
                        InputEnable = true;
                    };
                }
            }

            // Show stone head
            layout.Set80x80Picture(Picture80x80.Riddlemouth, 224, 49);
            var eyes = layout.AddAnimatedSprite(new Rect(240, 72, 48, 9), Graphics.RiddlemouthEyeIndex, UIPaletteIndex, 4);
            var mouth = layout.AddAnimatedSprite(new Rect(240, 90, 48, 15), Graphics.RiddlemouthMouthIndex, UIPaletteIndex, 7);

            if (showRiddle)
            {
                // Open eyes on start (and show the riddle)
                AddTimedEvent(TimeSpan.FromMilliseconds(250), () => HeadChangeEyes(true, () =>
                {
                    ShowRiddle();
                }));
            }
            else
            {
                // Eyes already open
                eyes.CurrentFrame = 3;
            }

            layout.AttachEventToButton(6, () => OpenDictionary(TestSolution));
            layout.AttachEventToButton(8, ShowRiddle);
            layout.AttachEventToButton(2, () => Exit(null));

            void Exit(Action? followAction)
            {
                HeadChangeEyes(false, () => CloseWindow(followAction));
            }

            void HeadChangeEyes(bool open, Action? followAction = null)
            {
                void NextFrame()
                {
                    void Next() => AddTimedEvent(TimeSpan.FromMilliseconds(150), NextFrame);

                    if (open)
                    {
                        if (++eyes.CurrentFrame == 3)
                            followAction?.Invoke();
                        else
                            Next();

                    }
                    else // close
                    {
                        if (--eyes.CurrentFrame == 0)
                            followAction?.Invoke();
                        else
                            Next();
                    }
                }

                NextFrame();
            }

            void HeadSpeak()
            {
                void NextFrame()
                {
                    void Next() => AddTimedEvent(TimeSpan.FromMilliseconds(150), NextFrame);

                    ++mouth.CurrentFrame;

                    // Note: The property will reset the frame to 0 when animation is done.
                    // But don't use an inline increment operator inside the if. This won't work!
                    if (mouth.CurrentFrame != 0)
                        Next();
                }

                mouth.CurrentFrame = 0;
                NextFrame();
            }
        });
    }

    internal Position GetPlayerDrawOffset(CharacterDirection? direction)
    {
        if (Map!.UseTravelTypes)
        {
            var travelInfo = renderView.GameData.GetTravelGraphicInfo(TravelType, direction ?? player!.Direction);

            return new Position((int)travelInfo.OffsetX - 16, (int)travelInfo.OffsetY - 16);
        }
        else
        {
            return new Position();
        }
    }

    internal Character2DAnimationInfo GetPlayerAnimationInfo(CharacterDirection? direction = null)
    {
        if (Map!.UseTravelTypes)
        {
            var travelInfo = renderView.GameData.GetTravelGraphicInfo(TravelType, direction ?? player!.Direction);

            return new Character2DAnimationInfo
            {
                FrameWidth = (int)travelInfo.Width,
                FrameHeight = (int)travelInfo.Height,
                StandFrameIndex = Graphics.TravelGraphicOffset + (uint)TravelType * 4,
                SitFrameIndex = 0,
                SleepFrameIndex = 0,
                NumStandFrames = 1,
                NumSitFrames = 0,
                NumSleepFrames = 0,
                TicksPerFrame = 0,
                NoDirections = false,
                IgnoreTileType = false,
                UseTopSprite = false
            };
        }
        else
        {
            var animationInfo = renderView.GameData.PlayerAnimationInfo;
            uint offset = (uint)Map.World * 17;
            animationInfo.StandFrameIndex += offset;
            animationInfo.SitFrameIndex += offset;
            animationInfo.SleepFrameIndex += offset;
            return animationInfo;
        }
    }

    internal bool IsNight()
    {
        return CurrentSavegame!.Hour >= 22 || CurrentSavegame.Hour < 5;
    }

    internal struct AutomapOptions
    {
        public bool SecretDoorsVisible;
        public bool MonstersVisible;
        public bool PersonsVisible;
        public bool TrapsVisible;
        public bool ShowGotoPoints;
    }

    struct AutomapWall
    {
        public int TileX;
        public int TileY;
        public int DrawX;
        public int DrawY;
        public bool? NormalWall; // true: normal, false: fake wall, null: wall with automap graphic on it
        public bool BlocksSight;
    }
}

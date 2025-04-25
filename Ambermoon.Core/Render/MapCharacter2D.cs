﻿/*
 * MapCharacter2D.cs - 2D map character implementation
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using System;
using System.Numerics;

namespace Ambermoon.Render
{
    using Flags = Map.CharacterReference.Flags;

    internal class MapCharacter2D : Character2D, IMapCharacter
    {
        static readonly Position NullOffset = new(0, 0);
        readonly Game game;
        readonly Map map;
        readonly Tileset tileset;
        readonly uint characterIndex;
        readonly Map.CharacterReference characterReference;
        uint lastTimeSlot = 0;
        uint? disallowInstantMovementUntilTimeSlot = null;
        uint lastInteractionTicks = 0;
        // This is used to avoid multiple monster encounters in the same update frame (e.g. 2 monsters move onto the player at the same time).
        static bool interacting = false;
        public bool CheckedIfSeesPlayer { get; set; } = false;
        public bool SeesPlayer { get; set; } = false;

        /// <summary>
        /// This is used to determine if a character is 1 or 2 tiles height in non-worldmaps.
        /// </summary>
        public bool IsRealCharacter => !characterReference.CharacterFlags.HasFlag(Flags.UseTileset);
        public bool IsMonster => characterReference.Type == CharacterType.Monster;
		public bool IsConversationPartner => characterReference.Type == CharacterType.PartyMember || characterReference.Type == CharacterType.NPC;
		public bool Paused { get; set; } = false;
        public Tileset.TileFlags TileFlags => characterReference?.TileFlags ?? Tileset.TileFlags.None;

        public static void Reset() => interacting = false;

        public void ResetLastInteractionTime() => lastInteractionTicks = game.CurrentTicks;

        private MapCharacter2D(Game game, IRenderView renderView, Layer layer, IMapManager mapManager,
            RenderMap2D map, uint characterIndex, Map.CharacterReference characterReference)
            : base(game, renderView.GetLayer(layer), TextureAtlasManager.Instance.GetOrCreate(layer),
                renderView.SpriteFactory, direction => AnimationProvider(game, map.Map, mapManager,
                    characterReference, renderView.GraphicProvider, direction), map,
                GetStartPosition(characterReference), () => Math.Max(1, map.Map.PaletteIndex) - 1, _ => NullOffset)
        {
            this.game = game;
            this.map = map.Map;
            tileset = mapManager.GetTilesetForMap(this.map);
            this.characterIndex = characterIndex;
            this.characterReference = characterReference;
            lastTimeSlot = game.GameTime.TimeSlot;
        }

        static Position GetStartPosition(Map.CharacterReference characterReference)
        {
            var position = characterReference.Positions[0];

            // The positions are stored 1-based.
            return new Position(position.X - 1, position.Y - 1);
        }

        static Character2DAnimationInfo AnimationProvider(Game game, Map map, IMapManager mapManager,
            Map.CharacterReference characterReference, IGraphicProvider graphicProvider, CharacterDirection? direction)
        {
            bool usesTileset = characterReference.CharacterFlags.HasFlag(Flags.UseTileset);

            if (usesTileset)
            {
                var tileset = mapManager.GetTilesetForMap(map);
                var tile = tileset.Tiles[characterReference.GraphicIndex - 1];

                return new Character2DAnimationInfo
                {
                    FrameWidth = RenderMap2D.TILE_WIDTH,
                    FrameHeight = RenderMap2D.TILE_HEIGHT,
                    StandFrameIndex = tile.GraphicIndex - 1,
                    SitFrameIndex = 0,
                    SleepFrameIndex = 0,
                    NumStandFrames = (uint)tile.NumAnimationFrames,
                    NumSitFrames = 0,
                    NumSleepFrames = 0,
                    TicksPerFrame = map.TicksPerAnimationFrame * 2,
                    NoDirections = true,
                    IgnoreTileType = true,
                    UseTopSprite = false
                };
            }
            else
            {
                var playerAnimationInfo = game.GetPlayerAnimationInfo(direction);
                return new Character2DAnimationInfo
                {
                    FrameWidth = 16, // NPC width
                    FrameHeight = 32, // NPC height
                    StandFrameIndex = Graphics.GetNPCGraphicIndex(map.NPCGfxIndex, characterReference.GraphicIndex, graphicProvider),
                    SitFrameIndex = playerAnimationInfo.SitFrameIndex,
                    SleepFrameIndex = playerAnimationInfo.SleepFrameIndex,
                    NumStandFrames = (uint)graphicProvider.NPCGraphicFrameCounts[(int)map.NPCGfxIndex][(int)characterReference.GraphicIndex],
                    NumSitFrames = playerAnimationInfo.NumSitFrames,
                    NumSleepFrames = playerAnimationInfo.NumSleepFrames,
                    TicksPerFrame = map.TicksPerAnimationFrame * 2,
                    NoDirections = true,
                    IgnoreTileType = false,
                    UseTopSprite = true
                };
            }
        }

        public static MapCharacter2D Create(Game game, IRenderView renderView, IMapManager mapManager,
            RenderMap2D map, uint characterIndex, Map.CharacterReference characterReference)
        {
            var layer = characterReference.CharacterFlags.HasFlag(Flags.UseTileset)
                ? (Layer)((uint)Layer.MapForeground1 + map.Map.TilesetOrLabdataIndex - 1) : Layer.Characters;
            return new MapCharacter2D(game, renderView, layer, mapManager, map, characterIndex, characterReference);
        }

        public override void Update(uint ticks, ITime gameTime,
			bool allowInstantMovement, Position lastPlayerPosition,
			MapAnimation mapAnimation, Tileset.TileFlags tileFlags)
        {
            if (!Active || Paused)
                return;

            Position newPosition = new Position(Position);

            bool TestPosition(Position position)
            {
                if (position == Position)
                    return false;

                var collisionPosition = new Position(position.X, position.Y);

                if (collisionPosition.X < 0 || collisionPosition.X >= map.Width)
                    return false;

                if (collisionPosition.Y < 0 || collisionPosition.Y >= map.Height)
                    return false;

                var mapEventId = Map[collisionPosition].MapEventId;

                if (mapEventId != 0)
                {
                    // Note: Won't work for world maps but there are no characters.
                    if (game.CurrentSavegame.IsEventActive(Map.Map.Index, mapEventId - 1))
                    {
                        switch (Map.Map.EventList[(int)mapEventId - 1].Type)
                        {
                            case EventType.Chest:
                            case EventType.Door:
                            case EventType.EnterPlace:
                            case EventType.Riddlemouth:
                            case EventType.Teleport:
                                return false;
                        }
                    }
                }

                // Note: Monsters and NPCs in 2D also use TravelType.Walk for collision detection.
                return Map[collisionPosition].AllowMovement(tileset, TravelType.Walk, false);
            }

            void MoveRandom()
            {
                for (int i = 0; i < 10; ++i) // limit to 10 tries to avoid infinite loops
                {
                    newPosition = new Position(Position.X + game.RandomInt(-1, 1), Position.Y + game.RandomInt(-1, 1));

                    if (TestPosition(newPosition))
                        break;

                    newPosition = Position;
                }
            }

            bool TestIfMonsterSeesPlayer()
            {
                CheckedIfSeesPlayer = true;
                return Map.MonsterSeesPlayer(newPosition);
            }

            if (characterReference.Type == CharacterType.Monster)
            {
                if ((CheckedIfSeesPlayer && SeesPlayer) ||
                    (!CheckedIfSeesPlayer && TestIfMonsterSeesPlayer()))
                {
                    game.MonsterSeesPlayer = true;
                    SeesPlayer = true;
                    CheckedIfSeesPlayer = true;

                    if (lastTimeSlot != gameTime.TimeSlot ||
                        (allowInstantMovement && (disallowInstantMovementUntilTimeSlot == null
                            || disallowInstantMovementUntilTimeSlot == gameTime.TimeSlot)))
                    {
                        disallowInstantMovementUntilTimeSlot = null;
                        var diff = game.RenderPlayer.Position - newPosition;
                        int dx = Math.Sign(diff.X);
                        int dy = Math.Sign(diff.Y);
                        if (Math.Abs(diff.X) <= 1 && Math.Abs(diff.Y) <= 1 && lastPlayerPosition != null)
                        {
                            var playerDiff = game.RenderPlayer.Position - lastPlayerPosition;
                            if ((playerDiff.X != 0 && playerDiff.Y == 0 && Math.Abs(diff.Y) != 0) ||
                                (playerDiff.X == 0 && playerDiff.Y != 0 && Math.Abs(diff.X) != 0))
                            {
                                dx = 0;
                            }
                        }
                        lastTimeSlot = gameTime.TimeSlot;
                        if (dx == 0)
                        {
                            newPosition.Y += dy;
                            if (!TestPosition(newPosition))
                                return; // Not moving
                        }
                        else if (dy == 0)
                        {
                            newPosition.X += dx;
                            if (!TestPosition(newPosition))
                                return; // Not moving
                        }
                        else
                        {
                            // Test with x and y change, then with only y change and then with only x change.
                            var position = newPosition + new Position(dx, dy);
                            if (TestPosition(position))
                                newPosition = position;
                            else
                            {
                                position.X = newPosition.X;
                                if (TestPosition(position))
                                    newPosition = position;
                                else
                                {
                                    newPosition.X += dx;
                                    if (!TestPosition(newPosition))
                                        return; // Not moving
                                }
                            }
                        }
                    }
                }
                else if (characterReference.CharacterFlags.HasFlag(Flags.RandomMovement) &&
                    !characterReference.OnlyMoveWhenSeePlayer)
                {
                    SeesPlayer = false;

                    if (lastTimeSlot != gameTime.TimeSlot)
                    {
                        MoveRandom();
                        lastTimeSlot = gameTime.TimeSlot;
                    }
                }
                else
                {
                    // Just stay
                    SeesPlayer = false;
                }
            }
            else
            {
                if (characterReference.CharacterFlags.HasFlag(Flags.RandomMovement))
                {
                    if (lastTimeSlot != gameTime.TimeSlot)
                    {
                        MoveRandom();
                        lastTimeSlot = gameTime.TimeSlot;
                    }
                }
                else if (!characterReference.Stationary)
                {
                    // Walk a given path every day time slot
                    newPosition = new Position(characterReference.Positions[(int)gameTime.TimeSlot % characterReference.Positions.Count]);
                    newPosition.Offset(-1, -1); // positions are 1-based
                }
            }

            base.MoveTo(map, (uint)newPosition.X, (uint)newPosition.Y, ticks, false, null);

            Update(ticks, gameTime, mapAnimation, characterReference.TileFlags);

            if (!interacting && IsMonster && newPosition == game.RenderPlayer.Position)
                Interact(EventTrigger.Move, false);
        }

        public bool CheckDeactivation(uint deactivatedEventIndex)
        {
            if (characterReference.EventIndex == deactivatedEventIndex)
            {
                if (Active && characterReference.Type == CharacterType.MapObject)
                    Deactivate();
                return true;
            }

            return false;
        }

        void Deactivate()
        {
            Active = false;
            game.CurrentSavegame.SetCharacterBit(map.Index, characterIndex, true);

            if (game.CurrentMapCharacter == this)
                game.CurrentMapCharacter = null;
        }

        public bool Interact(EventTrigger trigger, bool bed)
        {
            game.CurrentMapCharacter = null;

            switch (trigger)
            {
                case EventTrigger.Eye:
                case EventTrigger.Mouth:
                    if (characterReference.Type == CharacterType.Monster)
                        return false;
                    break;
                case EventTrigger.Move:
                    if (characterReference.Type != CharacterType.Monster &&
                        characterReference.Type != CharacterType.MapObject)
                        return false;
                    break;
                default:
                    return false;
            }

            if (trigger == EventTrigger.Mouth && bed &&
                !characterReference.CharacterFlags.HasFlag(Flags.UseTileset))
            {
                game.ShowMessagePopup(game.DataNameProvider.PersonAsleepMessage);
                return true;
            }

            bool TriggerCharacterEvents(uint eventIndex)
            {
                if ((long)game.CurrentTicks - lastInteractionTicks < Game.TicksPerSecond)
                    return false;

                var @event = map.EventList[(int)eventIndex - 1];

                if (@event is ConditionEvent conditionEvent)
                {
                    switch (conditionEvent.TypeOfCondition)
                    {
                        case ConditionEvent.ConditionType.Eye:
                            if (trigger != EventTrigger.Eye)
                                return false;
                            @event = conditionEvent.Next;
                            trigger = EventTrigger.Always;
                            break;
                        case ConditionEvent.ConditionType.Hand:
                            if (trigger != EventTrigger.Hand)
                                return false;
                            @event = conditionEvent.Next;
                            trigger = EventTrigger.Always;
                            break;
                        case ConditionEvent.ConditionType.UseItem:
                        {
                            if (trigger < EventTrigger.Item0)
                                return false;
                            var itemIndex = (uint)trigger - (uint)EventTrigger.Item0;
                            if (conditionEvent.ObjectIndex != itemIndex)
                                return false;
                            @event = conditionEvent.Next;
                            trigger = EventTrigger.Always;
                            break;
                        }
                    }
                }
                lastInteractionTicks = uint.MaxValue;
                interacting = true;
                game.CurrentMapCharacter = this;
                var position = game.RenderPlayer.Position;
                return EventExtensions.TriggerEventChain(map, game, trigger, (uint)position.X, (uint)position.Y, @event, true);
            }

            if (characterReference.CharacterFlags.HasFlag(Flags.TextPopup))
            {
                if (characterReference.EventIndex != 0 && game.CurrentSavegame.IsEventActive(map.Index, characterReference.EventIndex - 1))
                {
                    return TriggerCharacterEvents(characterReference.EventIndex);
                }
                else if (trigger == EventTrigger.Eye)
                {
                    // Popup NPCs can't be looked at but only talked to.
                    return false;
                }
                else if (trigger == EventTrigger.Mouth)
                {
                    ShowPopup(map.GetText((int)characterReference.Index, game.DataNameProvider.TextBlockMissing));
                    return true;
                }
            }

            bool HandleConversation(IConversationPartner conversationPartner)
            {
                if (trigger == EventTrigger.Eye)
                {
                    game.ShowMessagePopup(conversationPartner.Texts[0], null);
                    return true;
                }
                else if (trigger == EventTrigger.Mouth)
                {
                    if (conversationPartner == null)
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid NPC or party member index.");

                    conversationPartner.ExecuteEvents(game, trigger, characterIndex);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            switch (characterReference.Type)
            {
                case CharacterType.PartyMember:
                    return HandleConversation(game.CurrentSavegame.PartyMembers[characterReference.Index]);
                case CharacterType.NPC:
                    return HandleConversation(game.CharacterManager.GetNPC(characterReference.Index));
                case CharacterType.Monster:
                {
                    if (trigger == EventTrigger.Move)
                    {
                        if ((long)game.CurrentTicks - lastInteractionTicks < Game.TicksPerSecond)
                            return false;

                        if (game.Teleporting || game.Map != Map.Map)
                            return false;

                        if (game.Fading || game.PopupActive || game.CurrentWindow.Window != UI.Window.MapView)
                            return false;

                        // First set this to max so we won't trigger this again while we are interacting.
                        lastInteractionTicks = uint.MaxValue;
                        interacting = true;
                        game.CurrentMapCharacter = this;
                        var map = Map.GetMapFromTile((uint)Position.X, (uint)Position.Y);

                        void StartBattle(bool failedEscape)
                        {
                            game.StartBattle(characterReference.Index, failedEscape, (uint)game.PartyPosition.X, (uint)game.PartyPosition.Y, battleEndInfo =>
                            {
                                lastInteractionTicks = game.CurrentTicks;
                                interacting = false;
                                game.CurrentMapCharacter = null;

                                if (battleEndInfo.MonstersDefeated)
                                {
                                    Deactivate();
                                }
                                else
                                {
                                    Map.StopMonstersForOneTimeSlot();
                                }
                            }, characterReference.CombatBackgroundIndex);
                        }

                        game.ShowDecisionPopup(game.DataNameProvider.WantToFightMessage, response =>
                        {
                            if (response == PopupTextEvent.Response.Yes)
                            {
                                StartBattle(false);
                            }
                            else
                            {
                                var attributes = game.CurrentPartyMember.Attributes;
                                var dex = attributes[Data.Attribute.Dexterity].TotalCurrentValue;
                                var luk = attributes[Data.Attribute.Luck].TotalCurrentValue;
                                if (game.RandomInt(0, 149) >= dex + luk)
                                {
                                    StartBattle(true);
                                }
                                else
                                {
                                    // successfully fled
                                    lastInteractionTicks = game.CurrentTicks;
                                    interacting = false;
                                    game.CurrentMapCharacter = null;
                                    Map.StopMonstersForOneTimeSlot();
                                }
                            }
                        }, 2, 0, TextAlign.Left, false);
                    }
                    break;
                }
                case CharacterType.MapObject:
                    if (characterReference.EventIndex != 0 && game.CurrentSavegame.IsEventActive(map.Index, characterReference.EventIndex - 1))
                        return TriggerCharacterEvents(characterReference.EventIndex);
                    break;
            }

            return true;
        }

        public void StopMonsterForOneTimeSlot()
        {
            lastInteractionTicks = game.CurrentTicks;
            lastTimeSlot = game.GameTime.TimeSlot;
            disallowInstantMovementUntilTimeSlot = (lastTimeSlot + 1) % 288;
        }

        void ShowPopup(string text)
        {
            game.ShowMessagePopup(text, null, TextAlign.Center);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using TextColor = Ambermoon.Data.Enumerations.Color;
using Attribute = Ambermoon.Data.Attribute;

namespace Ambermoon;

partial class GameCore
{
    public const int MaxPartyMembers = 6;

    PartyMember? currentPickingActionMember = null;
    PartyMember? currentlyHealedMember = null;    
    bool pickingNewLeader = false;
    bool pickingTargetPlayer = false;
    bool pickingTargetInventory = false;
    bool partyAdvances = false; // party or monsters are advancing
    TravelType travelType = TravelType.Walk;

    event Action<int>? NewLeaderPicked;
    event Action<int>? TargetPlayerPicked;
    event Func<int, bool>? TargetInventoryPicked;
    event Func<ItemGrid?, int, ItemSlot?, bool>? TargetItemPicked;
    event Action? ActivePlayerChanged;

    public bool Godmode
    {
        get;
        set;
    } = false;
    public bool NoClip
    {
        get;
        set;
    } = false;
    public PartyMember? CurrentPartyMember { get; private set; } = null;
    internal PartyMember? CurrentInventory => CurrentInventoryIndex == null ? null : GetPartyMember(CurrentInventoryIndex.Value);
    internal int? CurrentInventoryIndex { get; set; } = null;
    internal Character? CurrentCaster { get; set; } = null;
    internal Character? CurrentSpellTarget { get; set; } = null;
    public Position PartyPosition => !Ingame || Map == null || player == null ? new Position() : LimitPartyPosition(Map.MapOffset + player.Position);

    internal TravelType TravelType
    {
        get => travelType;
        set
        {
            bool superman = travelType == TravelType.Fly || value == TravelType.Fly;
            travelType = value;
            CurrentSavegame!.TravelType = value;

            if (Map != null && Map.UseTravelMusic)
                PlayMusic(travelType.TravelSong());

            player!.MovementAbility = travelType.ToPlayerMovementAbility();

            if (Map?.UseTravelTypes == true)
            {
                player2D?.UpdateAppearance(CurrentTicks);
                GetTransportsInVisibleArea(out TransportLocation? transportAtPlayerIndex);

                if (player2D != null)
                {
                    player2D.BaselineOffset = !CanSee() || transportAtPlayerIndex != null ? MaxBaseLine :
                        player.MovementAbility > PlayerMovementAbility.Swimming ? 32 : 0;
                }
            }
            else if (!Is3D && player2D != null)
            {
                player2D.BaselineOffset = CanSee() ? 0 : MaxBaseLine;
            }

            if (Map != null && layout.ButtonGridPage == 1)
            {
                if (Map.Flags.HasFlag(MapFlags.CanRest) && travelType.CanCampOn())
                {
                    layout.EnableButton(5, true);
                }
                else
                {
                    layout.EnableButton(5, false);
                }
            }

            if (superman)
                UpdateLight();
        }
    }

    internal void SetPlayerDirection(CharacterDirection direction)
    {
        if (direction == CharacterDirection.Random)
            direction = (CharacterDirection)RandomInt(0, 3);

        CurrentSavegame!.CharacterDirection = direction;

        if (Is3D)
            player3D!.TurnTowards((int)direction * 90.0f);
        else
            player2D!.SetDirection(direction, CurrentTicks);
    }

    Position LimitPartyPosition(Position position)
    {
        if (Map == null)
            return position;

        int width = Map.IsWorldMap ? (int)Map.WorldMapDimension * 50 : Map.Width;
        int height = Map.IsWorldMap ? (int)Map.WorldMapDimension * 50 : Map.Height;

        while (position.X < 0)
            position.X += width;
        while (position.Y < 0)
            position.Y += height;

        position.X %= width;
        position.Y %= height;

        return position;
    }

    public bool CanRevive() => CurrentWindow.Window == Window.Camp;

    // Note: Eagle and wasp allow movement even with overweight.
    bool CanPartyMove() => TravelType == TravelType.Eagle || TravelType == TravelType.Wasp || !PartyMembers.Any(p => !p.CanMove(false));

    public IEnumerable<PartyMember> PartyMembers => Enumerable.Range(0, MaxPartyMembers)
        .Select(i => GetPartyMember(i)).Where(p => p != null).Cast<PartyMember>();

    public PartyMember? GetPartyMember(int slot) => CurrentSavegame?.GetPartyMember(slot);

    /// <summary>
    /// Runs an action for each party member. In contrast to a normal foreach loop
    /// the action can contain blocking calls for each party member like popups.
    /// The next party member is processed after an action is finished for the
    /// previous member.
    /// </summary>
    /// <param name="action">Action to perform. Second parameter is the finish handler the action must call.</param>
    /// <param name="condition">Condition to filter affected party members.</param>
    /// <param name="followUpAction">Action to trigger after all party members were processed.</param>
    internal void ForeachPartyMember(Action<PartyMember, Action> action, Func<PartyMember, bool>? condition = null,
        Action? followUpAction = null)
    {
        bool wasClickMoveActive = clickMoveActive;
        StartSequence();

        void Run(int index)
        {
            if (index == MaxPartyMembers)
            {
                Finish();
                return;
            }

            var partyMember = GetPartyMember(index);

            if (partyMember == null || condition?.Invoke(partyMember) == false)
            {
                Run(index + 1);
            }
            else
            {
                action(partyMember, () => Run(index + 1));
            }
        }

        Run(0);

        void Finish()
        {
            EndSequence();
            clickMoveActive = wasClickMoveActive;
            CurrentMobileAction = MobileAction.None;
            followUpAction?.Invoke();
        }
    }

    bool RecheckActivePartyMember(out bool gameOver)
    {
        gameOver = false;

        if (!CurrentPartyMember!.Conditions.CanSelect() || currentBattle?.GetSlotFromCharacter(CurrentPartyMember) == -1)
        {
            layout.ClearBattleFieldSlotColors();

            if (!PartyMembers.Any(p => p.Conditions.CanSelect()))
            {
                if (battleRoundActiveSprite != null)
                    battleRoundActiveSprite.Visible = false;
                currentBattleInfo = null;
                currentBattle = null;
                CloseWindow(() =>
                {
                    InputEnable = true;
                    Hook_GameOver();
                });
                gameOver = true;
                return true;
            }
            else if (BattleActive && !PartyMembers.Any(p => p.Conditions.CanSelect() && !currentBattle!.HasPartyMemberFled(p)))
            {
                // All dead or fled but at least one is still alive but fled.
                EndBattle(true);
                return false;
            }

            Pause();
            // Simple text popup
            var popup = layout.OpenTextPopup(ProcessText(DataNameProvider.SelectNewLeaderMessage), () =>
            {
                UntrapMouse();
                if (currentBattle == null && !WindowActive)
                    Resume();
                ResetCursor();
            }, true, false);
            popup.CanAbort = false;
            pickingNewLeader = true;
            CursorType = CursorType.Sword;
            TrapMouse(Global.PartyMemberPortraitArea);
            return false;
        }
        else
        {
            layout.UpdateCharacterNameColors(SlotFromPartyMember(CurrentPartyMember)!.Value);
            return true;
        }
    }

    internal void SetActivePartyMember(int index, bool updateBattlePosition = true)
    {
        var partyMember = GetPartyMember(index);

        bool TestConversationLanguage(WindowInfo windowInfo)
        {
            var conversationPartner = (windowInfo.WindowParameters[0] as IConversationPartner)!;

            if ((conversationPartner.SpokenLanguages & partyMember!.SpokenLanguages) == 0 &&
                (conversationPartner.SpokenExtendedLanguages & partyMember.SpokenExtendedLanguages) == 0)
            {
                ShowMessagePopup(DataNameProvider.YouDontSpeakSameLanguage);
                return false;
            }

            return true;
        }

        // This avoids switching to a player that doesn't speak the same language.
        if (CurrentWindow.Window == Window.Conversation && !TestConversationLanguage(CurrentWindow))
            return;
        if (LastWindow.Window == Window.Conversation && !TestConversationLanguage(LastWindow))
            return;
        if (PlayerIsPickingABattleAction)
            return;

        if (partyMember != null && (partyMember.Conditions.CanSelect() || currentWindow.Window == Window.Healer))
        {
            bool switched = CurrentPartyMember != partyMember;

            if (currentWindow.Window == Window.Healer)
            {
                currentlyHealedMember = partyMember;
                layout.SetCharacterHealSymbol(index);
            }
            else
            {
                if (HasPartyMemberFled(partyMember))
                    return;

                CurrentSavegame!.ActivePartyMemberSlot = index;
                currentPickingActionMember = CurrentPartyMember = partyMember;
                layout.SetActiveCharacter(index, Enumerable.Range(0, MaxPartyMembers).Select(GetPartyMember).ToList());
                layout.SetCharacterHealSymbol(null);

                if (currentBattle != null && updateBattlePosition && layout.Type == LayoutType.Battle)
                    BattlePlayerSwitched();

                if (pickingNewLeader)
                {
                    pickingNewLeader = false;
                    layout.ClosePopup(true, true);
                    ResetMoveKeys(true);
                    NewLeaderPicked?.Invoke(index);
                }

                if (is3D)
                    renderMap3D?.SetCameraHeight(partyMember.Race);
            }

            if (switched)
            {
                UpdateLight(false, false, true);

                if (!WindowActive)
                    layout.UpdateLayoutButtons();
            }

            ActivePlayerChanged?.Invoke();
        }
    }

    internal bool CanUseSpells()
    {
        if (Map?.CanUseSpells != true)
            return false;

        if (CurrentPartyMember?.Class.IsMagic() != true)
            return false;

        if (CurrentPartyMember?.Conditions.CanCastSpell(Features) != true)
            return false;

        return true;
    }

    internal bool CanSee() => !CurrentPartyMember!.Conditions.HasFlag(Condition.Blind) &&
        (!Map!.Flags.HasFlag(MapFlags.Dungeon) || lightIntensity > 0);

    public int AddAttributeDamageBonus(Character character, int attackDamage)
    {
        if (!Features.HasFlag(Features.AdjustedWeaponDamage))
        {
            return attackDamage + (int)character.Attributes[Attribute.Strength].TotalCurrentValue / 25;
        }
        else
        {
            var rightHandItemSlot = character.Equipment.Slots[EquipmentSlot.RightHand];

            if (rightHandItemSlot != null && rightHandItemSlot.ItemIndex != 0 && rightHandItemSlot.Amount != 0)
            {
                var rightHandItem = ItemManager.GetItem(rightHandItemSlot.ItemIndex);

                if (rightHandItem.Type == ItemType.LongRangeWeapon)
                {
                    return attackDamage +
                        (int)character.Attributes[Attribute.Dexterity].TotalCurrentValue / 25 +
                        (int)character.Attributes[Attribute.Strength].TotalCurrentValue / 50;
                }
            }

            return attackDamage +
                (int)character.Attributes[Attribute.Strength].TotalCurrentValue / 25 +
                (int)character.Attributes[Attribute.Dexterity].TotalCurrentValue / 50;
        }
    }

    public int AdjustAttackDamageForNotUsedAmmunition(Character character, int attackDamage)
    {
        var leftHandItemSlot = character.Equipment.Slots[EquipmentSlot.LeftHand];

        if (leftHandItemSlot != null && leftHandItemSlot.ItemIndex != 0 && leftHandItemSlot.Amount != 0)
        {
            var leftHandItem = ItemManager.GetItem(leftHandItemSlot.ItemIndex);

            if (leftHandItem.Type == ItemType.Ammunition && leftHandItem.Damage != 0)
            {
                var rightHandItemSlot = character.Equipment.Slots[EquipmentSlot.RightHand];

                if (rightHandItemSlot == null || rightHandItemSlot.ItemIndex == 0 || rightHandItemSlot.Amount == 0)
                    return attackDamage - leftHandItem.Damage;

                var rightHandItem = ItemManager.GetItem(rightHandItemSlot.ItemIndex);

                if (rightHandItem.UsedAmmunitionType != leftHandItem.AmmunitionType)
                    return attackDamage - leftHandItem.Damage;
            }
        }

        return attackDamage;
    }

    internal void DamageAllPartyMembers(Func<PartyMember, uint> damageProvider, Func<PartyMember, bool>? affectChecker = null,
        Action<PartyMember, Action>? notAffectedHandler = null, Action<bool>? followAction = null, Condition inflictCondition = Condition.None,
        bool showDamageSplash = true)
    {
        // In original all players are damaged one after the other
        // without showing the damage splash immediately. If a character
        // dies the skull is shown. If this was the active character
        // the "new leader" logic kicks in. Only after that the next
        // party member is checked.
        // At the end all affected living characters will show the damage splash.
        List<PartyMember> damagedPlayers = [];

        ForeachPartyMember(Damage, p => p.Alive && !p.Conditions.HasFlag(Condition.Petrified), () =>
        {
            if (showDamageSplash)
            {
                ForeachPartyMember(ShowDamageSplash, p => damagedPlayers.Contains(p), () =>
                {
                    layout.UpdateCharacterNameColors(CurrentSavegame!.ActivePartyMemberSlot);
                    followAction?.Invoke(damagedPlayers.Any(player => !player.Alive));
                });
            }
            else
            {
                layout.UpdateCharacterNameColors(CurrentSavegame!.ActivePartyMemberSlot);
                followAction?.Invoke(damagedPlayers.Any(player => !player.Alive));
            }
        });

        void Damage(PartyMember partyMember, Action finished)
        {
            if (affectChecker?.Invoke(partyMember) == false)
            {
                if (notAffectedHandler == null)
                    finished?.Invoke();
                else
                    notAffectedHandler?.Invoke(partyMember, finished);
                return;
            }

            var damage = Godmode ? 0 : damageProvider?.Invoke(partyMember) ?? 0;

            if (damage > 0 || inflictCondition != Condition.None)
            {
                partyMember.Damage(damage, _ => KillPartyMember(partyMember, Condition.DeadCorpse));

                if (!Godmode && partyMember.Alive && inflictCondition >= Condition.DeadCorpse)
                {
                    KillPartyMember(partyMember, inflictCondition);
                }

                if (partyMember.Alive) // update HP etc if not died already
                {
                    damagedPlayers.Add(partyMember);

                    if (!Godmode && inflictCondition != Condition.None && inflictCondition < Condition.DeadCorpse)
                    {
                        partyMember.Conditions |= inflictCondition;

                        if (inflictCondition == Condition.Blind && partyMember == CurrentPartyMember)
                            UpdateLight();
                    }
                }

                if (partyMember.Alive && partyMember.Conditions.CanSelect())
                {
                    finished?.Invoke();
                }
                else
                {
                    if (CurrentPartyMember == partyMember && currentBattle == null)
                    {
                        if (!PartyMembers.Any(p => p.Alive && p.Conditions.CanSelect()))
                        {
                            Hook_GameOver();
                            return;
                        }

                        bool inputWasEnabled = InputEnable;
                        bool allInputWasDisabled = allInputDisabled;
                        this.NewLeaderPicked += NewLeaderPicked;
                        allInputDisabled = false;
                        RecheckActivePartyMember(out bool gameOver);

                        if (gameOver || !pickingNewLeader)
                            this.NewLeaderPicked -= NewLeaderPicked;

                        if (gameOver)
                            allInputDisabled = false;
                        else if (!pickingNewLeader)
                            allInputDisabled = allInputWasDisabled;

                        void NewLeaderPicked(int index)
                        {
                            this.NewLeaderPicked -= NewLeaderPicked;
                            allInputDisabled = allInputWasDisabled;
                            finished?.Invoke();
                            InputEnable = inputWasEnabled;
                        }
                    }
                    else
                    {
                        layout.AttachToPortraitAnimationEvent(finished);
                    }
                }
            }
            else
            {
                finished?.Invoke();
            }
        }

        void ShowDamageSplash(PartyMember partyMember, Action finished) => this.ShowDamageSplash(partyMember, damageProvider, finished);
    }

    void DamageAllPartyMembers(uint damage, Func<PartyMember, bool>? affectChecker = null,
            Action<PartyMember, Action>? notAffectedHandler = null, Action<bool>? followAction = null)
    {
        DamageAllPartyMembers(_ => damage, affectChecker, notAffectedHandler, followAction);
    }

    void Levitate(Action failAction, bool climbIfNoEvent = true)
    {
        ConditionEvent? climbEvent = null;
        var levitatePosition = new Position(player!.Position);

        bool HasClimbEvent(uint x, uint y)
        {
            var mapEventId = Map!.Blocks[x, y].MapEventId;

            if (mapEventId == 0 || !CurrentSavegame!.IsEventActive(Map.Index, mapEventId - 1))
                return false;

            var @event = Map.EventList[(int)mapEventId - 1];

            if (@event is not ConditionEvent conditionEvent)
                return false;

            climbEvent = conditionEvent;

            return conditionEvent.TypeOfCondition == ConditionEvent.ConditionType.Levitating;
        }

        if (!HasClimbEvent((uint)player.Position.X, (uint)player.Position.Y))
        {
            // Also try forward position
            camera3D.GetForwardPosition(Global.DistancePerBlock, out float x, out float z, false, false);
            var position = Geometry.Geometry.CameraToBlockPosition(Map!, x, z);

            if (position == player.Position ||
                position.X < 0 || position.X >= Map!.Width ||
                position.Y < 0 || position.Y >= Map.Height ||
                !HasClimbEvent((uint)position.X, (uint)position.Y))
            {
                climbEvent = null;
            }
            else
            {
                levitatePosition = position;
            }
        }

        if (climbEvent != null)
        {
            // Attach player to ladder or hole
            float angle = camera3D.Angle;
            Geometry.Geometry.BlockToCameraPosition(Map!, levitatePosition, out float x, out float z);
            camera3D.SetPosition(-x, z);
            camera3D.TurnTowards(angle);
            camera3D.GetBackwardPosition(0.5f * Global.DistancePerBlock, out x, out z, false, false);
            camera3D.SetPosition(-x, z);
            camera3D.TurnTowards(angle);
        }

        if (climbIfNoEvent || climbEvent != null)
        {
            void StartClimbing()
            {
                Pause();
                Climb(() =>
                {
                    if (climbEvent == null)
                        failAction?.Invoke();
                    else
                    {
                        levitating = true;
                        EventExtensions.TriggerEventChain(Map!, this, EventTrigger.Levitating, (uint)levitatePosition.X,
                            (uint)levitatePosition.Y, climbEvent, true);
                    }
                });
            }
            if (WindowActive)
                CloseWindow(StartClimbing);
            else
                StartClimbing();
        }
        else
        {
            failAction?.Invoke();
        }
    }

    void Levitate()
    {
        Levitate(() =>
        {
            ShowMessagePopup(DataNameProvider.YouLevitate, () =>
            {
                MoveVertically(false, true, Resume);
            });
        });
    }

    void Climb(Action? finishAction = null)
    {
        MoveVertically(true, false, finishAction);
    }

    void Fall(uint tileX, uint tileY, Action? finishAction = null)
    {
        // Attach player to ladder or hole
        float angle = camera3D.Angle;
        Geometry.Geometry.BlockToCameraPosition(Map!, new Position((int)tileX, (int)tileY), out float x, out float z);
        camera3D.SetPosition(-x, z);
        camera3D.TurnTowards(angle);
        camera3D.GetBackwardPosition(0.45f * Global.DistancePerBlock, out x, out z, false, false);
        camera3D.SetPosition(-x, z);
        camera3D.TurnTowards(angle);

        MoveVertically(false, false, finishAction);
    }

    void MoveVertically(bool up, bool mapChange, Action? finishAction = null)
    {
        if (!Is3D || WindowActive)
        {
            finishAction?.Invoke();
            return;
        }

        var sourceY = !mapChange ? camera3D.Y : (up ? RenderMap3D.GetFloorY() : RenderMap3D.GetLevitatingY());
        player3D!.SetY(sourceY);
        var targetY = mapChange ? camera3D.GroundY : (up ? RenderMap3D.GetLevitatingY() : RenderMap3D.GetFloorY());
        float stepSize = RenderMap3D.GetLevitatingStepSize();
        float dist = Math.Abs(targetY - camera3D.Y);
        int steps = Math.Max(1, Util.Round(dist / stepSize));

        PlayTimedSequence(steps, () =>
        {
            if (up)
                camera3D.LevitateUp(stepSize);
            else
                camera3D.LevitateDown(stepSize);
        }, 75, finishAction);
    }

    /// <summary>
    /// Immediately moves 2 blocks forward.
    /// Can not pass walls.
    /// </summary>
    void Jump()
    {
        if (!is3D)
            return; // Should not happen

        if (WindowActive)
        {
            if (currentWindow.Window == Window.Inventory)
                CloseWindow(() => AddTimedEvent(TimeSpan.FromMilliseconds(250), Jump));
            return;
        }

        // Note: Even if the player looks diagonal (e.g. south west)
        // the jump is always performed into one of the 4 main directions.
        Position targetPosition = new(player3D!.Position);

        switch (player3D.Direction)
        {
            default:
            case CharacterDirection.Up:
                targetPosition.Y -= 2;
                break;
            case CharacterDirection.Right:
                targetPosition.X += 2;
                break;
            case CharacterDirection.Down:
                targetPosition.Y += 2;
                break;
            case CharacterDirection.Left:
                targetPosition.X -= 2;
                break;
        }

        var labdata = MapManager.GetLabdataForMap(Map);
        var checkPosition = new Position(player3D.Position);

        for (int i = 0; i < 2; ++i)
        {
            checkPosition.X += Math.Sign(targetPosition.X - checkPosition.X);
            checkPosition.Y += Math.Sign(targetPosition.Y - checkPosition.Y);

            if (Map!.Blocks[(uint)checkPosition.X, (uint)checkPosition.Y].BlocksPlayer(labdata, true))
            {
                ShowMessagePopup(DataNameProvider.CannotJumpThroughWalls);
                return;
            }

            var @event = Map.GetEvent((uint)checkPosition.X, (uint)checkPosition.Y, CurrentSavegame!);

            // Avoid jumping through closed doors, riddlemouths and place entrances.
            if (@event != null)
            {
                var trigger = EventTrigger.Move;
                bool lastEventStatus = true;
                bool aborted = false;

                while (@event is ConditionEvent condition)
                {
                    @event = condition.ExecuteEvent(Map, this, ref trigger,
                        (uint)checkPosition.X, (uint)checkPosition.Y, ref lastEventStatus,
                        out aborted, out _);

                    if (aborted)
                        break;
                }

                if (!aborted &&
                    ((@event is DoorEvent door && CurrentSavegame!.IsDoorLocked(door.DoorIndex)) ||
                    @event!.Type == EventType.Riddlemouth ||
                    @event.Type == EventType.EnterPlace))
                {
                    ShowMessagePopup(DataNameProvider.CannotJumpThroughWalls);
                    return;
                }
            }
        }

        player3D.SetPosition(targetPosition.X, targetPosition.Y, CurrentTicks, true);
        player3D.TurnTowards((float)player3D.Direction * 90.0f);
        camera3D.MoveBackward(0.35f * Global.DistancePerBlock, false, false);
    }

    internal void PlayerMoved(bool mapChange, Position? lastPlayerPosition = null, bool updateSavegame = true, Map? lastMap = null)
    {
        if (mapChange)
            lastMapTicksReset = CurrentTicks;

        if (updateSavegame)
        {
            var map = Is3D ? Map : renderMap2D!.GetMapFromTile((uint)player!.Position.X, (uint)player.Position.Y);
            CurrentSavegame!.CurrentMapIndex = map!.Index;
            CurrentSavegame.CurrentMapX = 1u + (uint)(player!.Position.X % Map!.Width);
            CurrentSavegame.CurrentMapY = 1u + (uint)(player.Position.Y % Map.Height);
            CurrentSavegame.CharacterDirection = player.Direction;
        }

        // Enable/disable transport button and show transports
        if (!WindowActive)
        {
            if (layout.ButtonGridPage == 1)
                layout.EnableButton(3, false);

            if (mapChange && Map!.Type == MapType.Map2D)
            {
                renderMap2D!.ClearTransports();

                if (player!.MovementAbility <= PlayerMovementAbility.Swimming)
                    player2D!.BaselineOffset = CanSee() ? 0 : MaxBaseLine;
            }

            void EnableTransport(bool enable = true)
            {
                layout.TransportEnabled = enable;
                if (layout.ButtonGridPage == 1)
                    layout.EnableButton(3, enable);
            }

            if (Map!.UseTravelTypes)
            {
                var transports = GetTransportsInVisibleArea(out TransportLocation? transportAtPlayerIndex);
                var tile = renderMap2D![player!.Position];
                var tileType = tile.Type;

                if (tileType == Map.TileType.Water && transportAtPlayerIndex != null &&
                    transportAtPlayerIndex.TravelType.CanStandOn())
                    tileType = Map.TileType.Normal;

                if (tileType == Map.TileType.Water)
                {
                    if (TravelType == TravelType.Walk)
                        StartSwimming();
                    else if (TravelType == TravelType.Swim)
                        DoSwimDamage();
                }
                else if (tileType != Map.TileType.Water && TravelType == TravelType.Swim)
                    TravelType = TravelType.Walk;

                var transportLocations = CurrentSavegame!.TransportLocations.ToList();
                foreach (var transport in transports)
                {
                    renderMap2D.PlaceTransport(transport.MapIndex,
                        (uint)transport.Position.X - 1, (uint)transport.Position.Y - 1, transport.TravelType, transportLocations.IndexOf(transport));
                }

                if (transportAtPlayerIndex != null && TravelType == TravelType.Walk)
                {
                    EnableTransport();
                    player2D!.BaselineOffset = MaxBaseLine;
                }
                else if (TravelType.IsStoppable() && transportAtPlayerIndex == null)
                {
                    // Only allow if we could stand or swim there.
                    var tileset = MapManager.GetTilesetForMap(renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y));

                    if (tile.AllowMovement(tileset, TravelType.Walk) ||
                        tile.AllowMovement(tileset, TravelType.Swim))
                        EnableTransport();
                    else
                        EnableTransport(false);
                }
                else
                {
                    EnableTransport(false);
                }
            }
            else
            {
                EnableTransport(false);
            }

            // Check auto poison
            if (!is3D && renderMap2D is not null && !TravelType.IgnoreAutoPoison())
            {
                var playerPosition = player!.Position;

                if (renderMap2D.IsTilePoisoning(playerPosition.X, playerPosition.Y))
                {
                    ForeachPartyMember((p, f) =>
                    {
                        if (RollDice100() >= p.Attributes[Data.Attribute.Luck].TotalCurrentValue)
                        {
                            AddCondition(Condition.Poisoned, p);
                            ShowDamageSplash(p, _ => 0, f);
                        }
                        else
                        {
                            f?.Invoke();
                        }
                    }, p => p.Alive && !p.Conditions.HasFlag(Condition.Petrified), () => ResetMoveKeys());
                }
            }

            UpdateMobileActionIndicatorPosition();
        }

        if (mapChange)
        {
            monstersCanMoveImmediately = false;
            if (lastMap == null || !lastMap.IsWorldMap ||
                !Map!.IsWorldMap || Map.World != lastMap.World)
                ResetMoveKeys(lastMap == null || lastMap.Type != Map!.Type);
            if (!WindowActive)
                layout.UpdateLayoutButtons(movement.MovementTicks(Map!.Type == MapType.Map3D, Map.UseTravelTypes, TravelType.Walk));

            // Update UI palette
            UpdateUIPalette(true);

            if (!Map!.IsWorldMap || TravelType == TravelType.Walk)
                PlayMapMusic();
        }
        else
        {
            this.lastPlayerPosition = lastPlayerPosition;
            monstersCanMoveImmediately = Map!.Type == MapType.Map2D && !Map.IsWorldMap;
        }

        if (Map.Type == MapType.Map3D)
        {
            // Explore
            if (!CurrentSavegame!.Automaps.TryGetValue(Map.Index, out var automap))
            {
                automap = new Automap { ExplorationBits = new byte[(Map.Width * Map.Height + 7) / 8] };
                CurrentSavegame.Automaps.Add(Map.Index, automap);
            }

            if (CanSee())
            {
                var labdata = MapManager.GetLabdataForMap(Map);

                for (int y = -1; y <= 1; ++y)
                {
                    for (int x = -1; x <= 1; ++x)
                    {
                        int totalX = player3D!.Position.X + x;
                        int totalY = player3D.Position.Y + y;

                        if (totalX < 0 || totalX >= Map.Width ||
                            totalY < 0 || totalY >= Map.Height)
                            continue;

                        automap.ExploreBlock(Map, (uint)totalX, (uint)totalY);

                        if (Map.Blocks[totalX, totalY].BlocksPlayerSight(labdata))
                            continue;

                        if (x != 0) // left or right column
                        {
                            int adjacentX = totalX + x;

                            if (adjacentX >= 0 && adjacentX < Map.Width)
                            {
                                for (int i = -1; i <= 1; ++i)
                                {
                                    int adjacentY = totalY + i;

                                    if (adjacentY >= 0 && adjacentY < Map.Height)
                                        automap.ExploreBlock(Map, (uint)adjacentX, (uint)adjacentY);
                                }
                            }
                        }
                        if (y != 0) // upper or lower row
                        {
                            int adjacentY = totalY + y;

                            if (adjacentY >= 0 && adjacentY < Map.Height)
                            {
                                for (int i = -1; i <= 1; ++i)
                                {
                                    int adjacentX = totalX + i;

                                    if (adjacentX >= 0 && adjacentX < Map.Width)
                                        automap.ExploreBlock(Map, (uint)adjacentX, (uint)adjacentY);
                                }
                            }
                        }
                        if (x != 0 && y != 0) // corners
                        {
                            int adjacentX = totalX + x;
                            int adjacentY = totalY + y;

                            if (adjacentX >= 0 && adjacentX < Map.Width &&
                                adjacentY >= 0 && adjacentY < Map.Height)
                                automap.ExploreBlock(Map, (uint)adjacentX, (uint)adjacentY);
                        }
                    }
                }
            }

            // Save goto points
            uint testX = 1u + (uint)player!.Position.X;
            uint testY = 1u + (uint)player.Position.Y;
            var gotoPoint = Map.GotoPoints.FirstOrDefault(p => p.X == testX && p.Y == testY);
            if (gotoPoint != null)
            {
                if (!CurrentSavegame.IsGotoPointActive(gotoPoint.Index))
                {
                    CurrentSavegame.ActivateGotoPoint(gotoPoint.Index);
                    ShowMessagePopup(DataNameProvider.GotoPointSaved, () =>
                    {
                        // If a goto point save message appears after map change,
                        // it will avoid triggering of map events so we have to call
                        // it on closing the popup.
                        if (mapChange)
                        {
                            TriggerMapEvents(EventTrigger.Move, (uint)this.player.Position.X,
                                (uint)this.player.Position.Y);
                        }

                    }, TextAlign.Left);
                    return;
                }
            }

            // Clairvoyance
            if (CurrentSavegame.IsSpellActive(ActiveSpellType.Clairvoyance))
            {
                bool trapFound = false;
                bool spinnerFound = false;
                player3D!.Camera.GetForwardPosition(1.05f * Global.DistancePerBlock, out float x, out float z, false, false);

                var checkPositions = new Position[2]
                {
                    player3D.Position,
                    Geometry.Geometry.CameraToBlockPosition(Map, x, z)
                };

                foreach (var checkPosition in checkPositions)
                {
                    if (checkPosition == lastPlayerPosition)
                        continue;

                    var type = renderMap3D!.FindEventTypesOnBlock((uint)checkPosition.X, (uint)checkPosition.Y, EventType.Trap, EventType.Spinner);

                    if (type == EventType.Trap)
                    {
                        trapFound = true;
                        break;
                    }
                    else if (type == EventType.Spinner)
                        spinnerFound = true;
                }

                if (trapFound)
                    ShowMessagePopup(DataNameProvider.YouNoticeATrap);
                else if (spinnerFound)
                    ShowMessagePopup(DataNameProvider.SeeRoundDiskInFloor);
            }
        }
    }

    internal void AddCondition(Condition condition, PartyMember? target = null)
    {
        if (Godmode)
            return;

        target ??= CurrentPartyMember!;

        if (condition >= Condition.DeadCorpse && target.Alive)
        {
            KillPartyMember(target, condition);
            return;
        }

        target.Conditions |= condition;

        if (CurrentPartyMember == target)
        {
            if (RecheckActivePartyMember(out bool gameOver))
            {
                if (gameOver)
                    return;

                if (condition == Condition.Blind)
                    UpdateLight();
            }
        }

        layout.UpdateCharacterNameColors(CurrentSavegame!.ActivePartyMemberSlot);
        layout.UpdateCharacter(target);
    }

    internal void RemoveCondition(Condition condition, Character target)
    {
        bool removeExhaustion = condition == Condition.Exhausted && target.Conditions.HasFlag(Condition.Exhausted);

        // Healing spells or potions.
        // Sleep can be removed by attacking as well.
        target.Conditions &= ~condition;

        if (target is PartyMember partyMember)
        {
            if (BattleActive)
            {
                UpdateBattleStatus(partyMember);
                currentBattle!.RemoveCondition(condition, target);
            }
            layout.UpdateCharacterNameColors(CurrentSavegame!.ActivePartyMemberSlot);
            layout.UpdateCharacter(partyMember);

            if (removeExhaustion)
                RemoveExhaustion(partyMember);
            else if (condition == Condition.Blind && partyMember == CurrentPartyMember)
                UpdateLight();
        }
    }

    uint AddExhaustion(PartyMember partyMember, uint hours, bool crippleAttributes)
    {
        uint totalDamage = 0;
        long hitPoints = partyMember.HitPoints.CurrentValue;

        for (uint i = 0; i < hours; ++i)
        {
            // Do at least 1 damage per hour
            uint damage = Math.Max(1, (uint)hitPoints / 10);
            totalDamage += damage;
            hitPoints -= damage;

            if (hitPoints <= 0)
                break;
        }

        if (crippleAttributes && hitPoints > 0)
        {
            foreach (var attribute in EnumHelper.GetValues<Attribute>())
            {
                partyMember.Attributes[attribute].StoredValue = partyMember.Attributes[attribute].CurrentValue;
                partyMember.Attributes[attribute].CurrentValue >>= 1;
            }

            foreach (var skill in EnumHelper.GetValues<Skill>())
            {
                partyMember.Skills[skill].StoredValue = partyMember.Skills[skill].CurrentValue;
                partyMember.Skills[skill].CurrentValue >>= 1;
            }
        }

        return Math.Min(totalDamage, partyMember.HitPoints.CurrentValue);
    }

    void RemoveExhaustion(PartyMember partyMember)
    {
        foreach (var attribute in EnumHelper.GetValues<Attribute>())
        {
            partyMember.Attributes[attribute].CurrentValue = partyMember.Attributes[attribute].StoredValue;
            partyMember.Attributes[attribute].StoredValue = 0;
        }

        foreach (var skill in EnumHelper.GetValues<Skill>())
        {
            partyMember.Skills[skill].CurrentValue = partyMember.Skills[skill].StoredValue;
            partyMember.Skills[skill].StoredValue = 0;
        }
    }

    public void ActivateLight(uint level)
    {
        ActivateLight(180, level);
    }

    void ActivateLight(uint duration, uint level)
    {
        CurrentSavegame!.ActivateSpell(ActiveSpellType.Light, duration, level);
        UpdateLight(false, true);
    }

    internal void ActivateBuff(ActiveSpellType buff, uint value, uint duration)
    {
        if (buff == ActiveSpellType.Light)
            ActivateLight(duration, value);
        else
            CurrentSavegame!.ActivateSpell(buff, duration, value);
    }

    public void Revive(Character caster, List<PartyMember> affectedMembers, Action? finishAction = null)
    {
        void Revive(PartyMember target, Action finishAction) =>
            ApplySpellEffect(Spell.Resurrection, caster, target, finishAction, false);

        ForeachPartyMember(Revive, p => affectedMembers.Contains(p) && p.Conditions.HasFlag(Condition.DeadCorpse), () =>
        {
            currentAnimation?.Destroy();
            currentAnimation = new SpellAnimation(this, layout);

            currentAnimation.CastHealingOnPartyMembers(() =>
            {
                currentAnimation.Destroy();
                currentAnimation = null;
                finishAction?.Invoke();
            }, affectedMembers);
        });
    }

    void PartyMemberDied(Character partyMember)
    {
        if (partyMember is not PartyMember member)
            throw new AmbermoonException(ExceptionScope.Application, "PartyMemberDied with a character which is not a party member.");

        member.HitPoints.CurrentValue = 0;

        int? slot = SlotFromPartyMember(member);

        if (slot != null)
            layout.SetCharacter(slot.Value, member, false, () => ResetMoveKeys(true));
    }

    void PartyMemberRevived(PartyMember partyMember, Action? finishAction = null, bool showHealAnimation = true, bool selfRevive = false)
    {
        string reviveMessage = selfRevive && partyMember.Race == Race.Animal && !string.IsNullOrWhiteSpace(DataNameProvider.ReviveCatMessage) ? DataNameProvider.ReviveCatMessage : DataNameProvider.ReviveMessage;

        if (CurrentWindow.Window == Window.Healer)
        {
            layout.UpdateCharacter(partyMember, () => layout.ShowClickChestMessage(reviveMessage, finishAction));
        }
        else
        {
            bool allInputWasDisabled = allInputDisabled;
            allInputDisabled = false;

            ShowMessagePopup(reviveMessage, () =>
            {
                allInputDisabled = allInputWasDisabled;

                void Finish()
                {
                    if (showHealAnimation)
                    {
                        currentAnimation?.Destroy();
                        currentAnimation = new SpellAnimation(this, layout);
                        // This will just show the heal animation
                        currentAnimation.CastOn(Spell.SelfHealing, partyMember, () =>
                        {
                            currentAnimation.Destroy();
                            currentAnimation = null;
                            finishAction?.Invoke();
                        });
                    }
                    else
                    {
                        finishAction?.Invoke();
                    }
                }

                layout.SetCharacter(SlotFromPartyMember(partyMember)!.Value, partyMember, false, Finish);

                if (currentWindow.Window == Window.Inventory && partyMember == CurrentInventory)
                    UpdateCharacterInfo();

                layout.FillCharacterBars(partyMember);
            });
        }
    }

    void FixPartyMember(PartyMember partyMember)
    {
        // Don't do it for animals though!
        if (partyMember.Race > Race.Thalionic)
            return;

        // The original has some bugs where bonus values are not right.
        // We set the bonus values here dependent on equipment.
        partyMember.HitPoints.BonusValue = 0;
        partyMember.SpellPoints.BonusValue = 0;
        partyMember.BonusDefense = 0;
        partyMember.BonusAttackDamage = 0;

        foreach (var attribute in EnumHelper.GetValues<Attribute>())
        {
            partyMember.Attributes[attribute].BonusValue = 0;
        }

        foreach (var skill in EnumHelper.GetValues<Skill>())
        {
            partyMember.Skills[skill].BonusValue = 0;
        }

        foreach (var itemSlot in partyMember.Equipment.Slots)
        {
            if (itemSlot.Value.ItemIndex != 0)
            {
                var item = ItemManager.GetItem(itemSlot.Value.ItemIndex);
                int factor = itemSlot.Value.Flags.HasFlag(ItemSlotFlags.Cursed) ? -1 : 1;

                partyMember.HitPoints.BonusValue += factor * item.HitPoints;
                partyMember.SpellPoints.BonusValue += factor * item.SpellPoints;
                partyMember.BonusDefense = (short)(partyMember.BonusDefense + factor * item.Defense);
                partyMember.BonusAttackDamage = (short)(partyMember.BonusAttackDamage + factor * item.Damage);

                if (item.Attribute != null)
                    partyMember.Attributes[item.Attribute.Value].BonusValue += factor * item.AttributeValue;
                if (item.Skill != null)
                    partyMember.Skills[item.Skill.Value].BonusValue += factor * item.SkillValue;
            }
        }

        if (!Features.HasFlag(Features.AdvancedAPRCalculation))
            partyMember.AttacksPerRound = (byte)(partyMember.AttacksPerRoundIncreaseLevels == 0 ? 1 : Util.Limit(partyMember.AttacksPerRound, partyMember.Level / partyMember.AttacksPerRoundIncreaseLevels, 255));
        else
            partyMember.AttacksPerRound = (byte)(partyMember.AttacksPerRoundIncreaseLevels == 0 ? 1 : Util.Limit(partyMember.AttacksPerRound, 1 + partyMember.Level / partyMember.AttacksPerRoundIncreaseLevels, 255));
    }

    /// <summary>
    /// Is used for external cheats.
    /// </summary>
    /// <param name="partyMember">The party member to add</param>
    /// <returns>0: Success, -1: Wrong window, -2: No free slot</returns>
    public int AddPartyMember(PartyMember partyMember)
    {
        if (CurrentWindow.Window != Window.MapView || WindowActive)
        {
            return -1; // Wrong window
        }

        for (int i = 0; i < MaxPartyMembers; ++i)
        {
            if (GetPartyMember(i) == null)
            {
                CurrentSavegame!.CurrentPartyMemberIndices[i] =
                    CurrentSavegame.PartyMembers.FirstOrDefault(p => p.Value == partyMember).Key;
                this.AddPartyMember(i, partyMember, null, true);
                // Set battle position
                CurrentSavegame.BattlePositions[i] = 0xff;
                var usePositions = CurrentSavegame.BattlePositions.ToList();
                for (int p = 11; p >= 0; --p)
                {
                    if (!usePositions.Contains((byte)p))
                    {
                        CurrentSavegame.BattlePositions[i] = (byte)p;
                        break;
                    }
                }
                ushort characterBit;
                if (IsMapCharacterActive(PartyMemberInitialCharacterBits[partyMember.Index]))
                    characterBit = PartyMemberInitialCharacterBits[partyMember.Index];
                else
                    characterBit = PartyMemberCharacterBits[partyMember.Index];
                SetMapCharacterBit(characterBit, true);
                if (partyMember.CharacterBitIndex == 0xffff || partyMember.CharacterBitIndex == 0x0000)
                    partyMember.CharacterBitIndex = characterBit;
                return 0;
            }
        }

        return -2; // No free slot
    }

    void AddPartyMember(int slot, PartyMember partyMember, Action? followAction = null, bool forceAnimation = false)
    {
        FixPartyMember(partyMember);
        partyMember.Died += PartyMemberDied;
        layout.SetCharacter(slot, partyMember, false, followAction, forceAnimation);
        spellListScrollOffsets[slot] = 0;
    }

    internal void RemovePartyMember(int slot, bool initialize, Action? followAction = null)
    {
        var partyMember = GetPartyMember(slot);

        if (partyMember != null)
            partyMember.Died -= PartyMemberDied;

        layout.SetCharacter(slot, null, initialize, followAction);
        spellListScrollOffsets[slot] = 0;
    }

    void ClearPartyMembers()
    {
        for (int i = 0; i < GameCore.MaxPartyMembers; ++i)
            RemovePartyMember(i, true);
    }

    internal int? SlotFromPartyMember(PartyMember partyMember)
    {
        for (int i = 0; i < MaxPartyMembers; ++i)
        {
            if (GetPartyMember(i) == partyMember)
                return i;
        }

        return null;
    }

    internal void ProcessPoisonDamage(uint times, Action<bool>? followAction = null)
    {
        uint GetDamage()
        {
            uint damage = 0;

            for (uint i = 0; i < times; ++i)
                damage += (uint)RandomInt(1, 5);

            return damage;
        }

        DamageAllPartyMembers(_ => GetDamage(),
            p => p.Alive && p.Conditions.HasFlag(Condition.Poisoned), null, followAction);
    }

    void Sleep(bool inn, int healing)
    {
        healing = Util.Limit(0, healing, 100);

        for (int i = 0; i < MaxPartyMembers; ++i)
        {
            var partyMember = GetPartyMember(i);

            if (partyMember != null && partyMember.Alive)
            {
                if (partyMember.Conditions.HasFlag(Condition.Exhausted))
                {
                    partyMember.Conditions &= ~Condition.Exhausted;
                    RemoveExhaustion(partyMember);
                    layout.UpdateCharacterStatus(partyMember);
                }
            }
        }

        void Start(bool toDawn)
        {
            // Set this first to avoid tired/exhausted warning when increasing the game time.
            GameTime!.HoursWithoutSleep = 0;
            uint hoursToAdd = 8;
            uint minutesToAdd = 0;

            if (toDawn)
            {
                if (GameTime.Hour >= 20) // move to next day
                {
                    hoursToAdd = 7 + 24 - GameTime.Hour - 1;
                    minutesToAdd = 60 - GameTime.Minute % 60;
                }
                else
                {
                    hoursToAdd = 7 - GameTime.Hour - 1;
                    minutesToAdd = 60 - GameTime.Minute % 60;
                }
            }

            GameTime.Wait(hoursToAdd);

            while (minutesToAdd > 0)
            {
                minutesToAdd -= 5;
                GameTime.Tick();
            }

            // Set this again to reset it after game time was increased.
            GameTime.HoursWithoutSleep = 0; // This also resets it inside the savegame.

            // Recovery and food consumption
            void Recover(int slot)
            {
                void Next() => Recover(slot + 1);

                if (slot < MaxPartyMembers)
                {
                    var partyMember = GetPartyMember(slot);

                    if (partyMember != null && partyMember.Alive)
                    {
                        if (!inn && partyMember.Food == 0 && partyMember.Race < Race.Animal)
                        {
                            layout.ShowClickChestMessage(partyMember.Name + DataNameProvider.HasNoMoreFood, Next);
                        }
                        else
                        {
                            int lpRecovered = Util.Limit(0, healing * (int)partyMember.HitPoints.TotalMaxValue / 100,
                                (int)partyMember.HitPoints.TotalMaxValue - (int)partyMember.HitPoints.CurrentValue);
                            partyMember.HitPoints.CurrentValue += (uint)lpRecovered;
                            int spRecovered = Util.Limit(0, healing * (int)partyMember.SpellPoints.TotalMaxValue / 100,
                                (int)partyMember.SpellPoints.TotalMaxValue - (int)partyMember.SpellPoints.CurrentValue);
                            partyMember.SpellPoints.CurrentValue += (uint)spRecovered;
                            layout.FillCharacterBars(partyMember);

                            if (!inn && partyMember.Race < Race.Animal)
                                --partyMember.Food;

                            if (partyMember.Class.IsMagic() && spRecovered != 0) // Has SP and was recovered
                            {
                                layout.ShowClickChestMessage(partyMember.Name + string.Format(DataNameProvider.RecoveredLPAndSP, lpRecovered, spRecovered), Next);
                            }
                            else
                            {
                                layout.ShowClickChestMessage(partyMember.Name + string.Format(DataNameProvider.RecoveredLP, lpRecovered), Next);
                            }
                        }
                    }
                    else
                    {
                        Next();
                    }
                }
            }
            Recover(0);
        }

        if (!inn && !Map!.Flags.HasFlag(MapFlags.NoSleepUntilDawn) &&
            (GameTime!.Hour >= 20 || GameTime.Hour < 4)) // Sleep until dawn
        {
            layout.ShowClickChestMessage(DataNameProvider.SleepUntilDawn, () => Start(true));
        }
        else // sleep 8 hours
        {
            layout.ShowClickChestMessage(DataNameProvider.Sleep8Hours, () => Start(false));
        }
    }

    void AgePlayer(PartyMember partyMember, Action finishAction, uint ageIncrease)
    {
        partyMember.Attributes[Attribute.Age].CurrentValue += ageIncrease;

        bool allInputWasDisabled = allInputDisabled;
        allInputDisabled = false;

        void Finish()
        {
            allInputDisabled = allInputWasDisabled;
            finishAction?.Invoke();
        }

        if (partyMember.Attributes[Attribute.Age].CurrentValue >= partyMember.Attributes[Attribute.Age].MaxValue)
        {
            partyMember.Attributes[Attribute.Age].CurrentValue = partyMember.Attributes[Attribute.Age].MaxValue;
            ShowMessagePopup(partyMember.Name + DataNameProvider.HasDiedOfAge, () =>
            {
                KillPartyMember(partyMember);
                Finish();
            });
        }
        else
        {
            ShowMessagePopup(partyMember.Name + DataNameProvider.HasAged, Finish);
        }
    }

    public void KillPartyMember(PartyMember partyMember, Condition deathCondition = Condition.DeadCorpse)
    {
        RemoveCondition(Condition.Exhausted, partyMember);
        partyMember.Die(deathCondition);
    }

    // Note: Only used external for cheats
    public void RecheckActivePlayer()
    {
        if (RecheckActivePartyMember(out bool gameOver))
        {
            if (gameOver || !BattleActive)
                return;
            BattlePlayerSwitched();
        }
        else if (BattleActive)
        {
            AddCurrentPlayerActionVisuals();
        }
    }

    internal uint DistributeGold(uint gold, bool force)
    {
        var partyMembers = PartyMembers.Where(p => p.Race != Race.Animal).ToList();

        while (gold != 0)
        {
            int numTargetPlayers = partyMembers.Count;
            uint goldPerPlayer = gold / (uint)numTargetPlayers;
            bool anyCouldTake = false;

            if (goldPerPlayer == 0)
            {
                numTargetPlayers = (int)gold;
                goldPerPlayer = 1;
            }

            foreach (var partyMember in partyMembers)
            {
                uint goldToTake = force ? goldPerPlayer : Math.Min(partyMember.MaxGoldToTake, goldPerPlayer);
                gold -= goldToTake;
                partyMember.AddGold(goldToTake);

                if (goldToTake != 0)
                {
                    anyCouldTake = true;

                    if (--numTargetPlayers == 0)
                        break;
                }
            }

            if (!anyCouldTake)
                return gold;
        }

        return gold;
    }

    internal uint DistributeFood(uint food, bool force)
    {
        var partyMembers = PartyMembers.Where(p => p.Race != Race.Animal).ToList();

        while (food != 0)
        {
            int numTargetPlayers = partyMembers.Count;
            uint foodPerPlayer = food / (uint)numTargetPlayers;
            bool anyCouldTake = false;

            if (foodPerPlayer == 0)
            {
                numTargetPlayers = (int)food;
                foodPerPlayer = 1;
            }

            foreach (var partyMember in partyMembers)
            {
                uint foodToTake = force ? foodPerPlayer : Math.Min(partyMember.MaxFoodToTake, foodPerPlayer);
                food -= foodToTake;
                partyMember.AddFood(foodToTake);

                if (foodToTake != 0)
                {
                    anyCouldTake = true;

                    if (--numTargetPlayers == 0)
                        break;
                }
            }

            if (!anyCouldTake)
                return food;
        }

        return food;
    }

    internal void SpeakToParty()
    {
        var hero = GetPartyMember(0)!;

        if (!hero.Alive || !hero.Conditions.CanTalk())
        {
            ShowMessagePopup(DataNameProvider.UnableToTalk);
            return;
        }
        if (CurrentSavegame!.ActivePartyMemberSlot != 0)
            SetActivePartyMember(0);

        Pause();
        layout.OpenTextPopup(ProcessText(DataNameProvider.WhoToTalkTo),
            null, true, false, false, TextAlign.Center);
        PickTargetPlayer();

        void TargetPlayerPicked(int characterSlot)
        {
            ResetMoveKeys(true);

            if (characterSlot != -1)
            {
                var partyMember = GetPartyMember(characterSlot)!;

                if (!partyMember.Alive || partyMember.Conditions.HasFlag(Condition.Petrified))
                {
                    ExecuteNextUpdateCycle(PickTargetPlayer);
                    return;
                }
            }

            this.TargetPlayerPicked -= TargetPlayerPicked;
            ClosePopup();
            UntrapMouse();
            InputEnable = true;

            if (!WindowActive)
                Resume();

            if (characterSlot != -1)
            {
                if (characterSlot == 0)
                    ExecuteNextUpdateCycle(() => ShowMessagePopup(DataNameProvider.SelfTalkingIsMad));
                else
                {
                    var partyMember = GetPartyMember(characterSlot)!;

                    ExecuteNextUpdateCycle(() => ShowConversation(partyMember, null, null, new ConversationItems()));
                }
            }
        }
        this.TargetPlayerPicked += TargetPlayerPicked;
    }

    void PickTargetPlayer()
    {
        pickingTargetPlayer = true;
        CursorType = CursorType.Sword;
        TrapMouse(Global.PartyMemberPortraitArea);
    }

    void PickTargetInventory()
    {
        pickingTargetInventory = true;
        CursorType = CursorType.Sword;
        TrapMouse(Global.PartyMemberPortraitArea);
    }

    internal void FinishPickingTargetPlayer(int characterSlot)
    {
        TargetPlayerPicked?.Invoke(characterSlot);
        pickingTargetPlayer = false;
        UntrapMouse();
    }

    internal void AbortPickingTargetPlayer()
    {
        pickingTargetPlayer = false;
        TargetPlayerPicked?.Invoke(-1);
        ClosePopup();
    }

    internal bool FinishPickingTargetInventory(int characterSlot)
    {
        bool result = TargetInventoryPicked?.Invoke(characterSlot) ?? true;

        if (!result)
        {
            pickingTargetInventory = false;

            if (currentWindow.Window == Window.Inventory)
                CloseWindow();

            layout.ShowChestMessage(null);
            UntrapMouse();
        }

        return result;
    }

    internal void FinishPickingTargetInventory(ItemGrid itemGrid, int slotIndex, ItemSlot itemSlot)
    {
        pickingTargetInventory = false;

        if (TargetItemPicked?.Invoke(itemGrid, slotIndex, itemSlot) != false)
        {
            if (currentWindow.Window == Window.Inventory)
                CloseWindow();

            layout.ShowChestMessage(null);
            ClosePopup();
            UntrapMouse();
        }
    }

    internal void AbortPickingTargetInventory()
    {
        pickingTargetInventory = false;

        if (TargetInventoryPicked?.Invoke(-1) != false)
        {
            if (TargetItemPicked?.Invoke(null, 0, null) != false)
            {
                if (currentWindow.Window == Window.Inventory)
                    CloseWindow();

                layout.ShowChestMessage(null);
                ClosePopup();
                EndSequence();
                UntrapMouse();
            }
        }
    }

    internal void OpenCamp(bool inn, int healing = 50) // 50 when camping outside of inns
    {
        if (!inn && MonsterSeesPlayer)
        {
            ShowMessagePopup(DataNameProvider.RestingTooDangerous);
            return;
        }

        Fade(() =>
        {
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Camp, inn, healing);
            lastPlayedSong = PlayMusic(Song.BarBrawlin);
            layout.SetLayout(LayoutType.Items);
            layout.Set80x80Picture(inn ? Picture80x80.RestInn : Map!.Flags.HasFlag(MapFlags.Outdoor) ? Picture80x80.RestOutdoor : Picture80x80.RestDungeon);
            layout.FillArea(new Rect(110, 43, 194, 80), GetUIColor(28), false);
            var itemSlotPositions = Enumerable.Range(1, 6).Select(index => new Position(index * 22, 139)).ToList();
            itemSlotPositions.AddRange(Enumerable.Range(1, 6).Select(index => new Position(index * 22, 168)));
            var itemGrid = ItemGrid.Create(this, layout, renderView, ItemManager, itemSlotPositions, Enumerable.Repeat(null as ItemSlot, 24).ToList(),
                false, 12, 6, 24, new Rect(7 * 22, 139, 6, 53), new Size(6, 27), ScrollbarType.SmallVertical);
            itemGrid.Disabled = true;
            layout.AddItemGrid(itemGrid);
            var itemArea = new Rect(16, 139, 151, 53);

            void PlayerSwitched()
            {
                itemGrid.HideTooltip();
                itemGrid.Disabled = true;
                layout.ShowChestMessage(null);
                UntrapMouse();
                CursorType = CursorType.Sword;
                inputEnable = true;
                bool magicClass = CurrentPartyMember!.Class.IsMagic();
                layout.EnableButton(0, magicClass);
                layout.EnableButton(3, magicClass);
            }

            ActivePlayerChanged += PlayerSwitched;
            closeWindowHandler = _ => ActivePlayerChanged -= PlayerSwitched;

            void Exit()
            {
                CloseWindow();
            }

            // exit button
            layout.AttachEventToButton(2, Exit);

            // use magic button
            layout.AttachEventToButton(0, () => CastSpell(true, itemGrid));

            void SetupRightClickAbort()
            {
                nextClickHandler = buttons =>
                {
                    if (buttons == MouseButtons.Right)
                    {
                        itemGrid.HideTooltip();
                        itemGrid.Disabled = true;
                        layout.ShowChestMessage(null);
                        UntrapMouse();
                        layout.ButtonsDisabled = false;
                        CursorType = CursorType.Sword;
                        inputEnable = true;
                        return true;
                    }

                    return false;
                };
            }

            // read magic button
            layout.AttachEventToButton(3, () =>
            {
                layout.ShowChestMessage(DataNameProvider.WhichScrollToRead, TextAlign.Left);
                itemGrid.Disabled = false;
                itemGrid.DisableDrag = true;
                CursorType = CursorType.Sword;
                TrapMouse(itemArea);
                layout.ButtonsDisabled = true;
                itemGrid.Initialize(CurrentPartyMember!.Inventory.Slots.ToList(), false);
                SetupRightClickAbort();
            });

            // sleep button
            layout.AttachEventToButton(6, () =>
            {
                if (!inn && CurrentSavegame!.HoursWithoutSleep < 8)
                {
                    layout.ShowClickChestMessage(DataNameProvider.RestingWouldHaveNoEffect);
                }
                else
                {
                    Sleep(inn, healing);
                }
            });

            itemGrid.ItemClicked += (ItemGrid _, int slotIndex, ItemSlot itemSlot) =>
            {
                itemGrid.HideTooltip();

                void ShowMessage(string message, Action? additionalAction = null)
                {
                    nextClickHandler = null;
                    layout.ShowClickChestMessage(message, () =>
                    {
                        layout.ShowChestMessage(DataNameProvider.WhichScrollToRead);
                        additionalAction?.Invoke();
                        TrapMouse(itemArea);
                        SetupRightClickAbort();
                    });
                }

                // This is only used in "read magic".
                var item = ItemManager.GetItem(itemSlot.ItemIndex);

                if (item.Type != ItemType.SpellScroll || item.Spell == Spell.None)
                {
                    ShowMessage(DataNameProvider.ThatsNotASpellScroll);
                }
                else if (item.SpellSchool != CurrentPartyMember!.Class.ToSpellSchool())
                {
                    ShowMessage(DataNameProvider.CantLearnSpellsOfType);
                }
                else if (CurrentPartyMember.HasSpell(item.Spell))
                {
                    ShowMessage(DataNameProvider.AlreadyKnowsSpell);
                }
                else
                {
                    uint slpCost = SpellInfos.GetSLPCost(Features, item.Spell);

                    if (CurrentPartyMember.SpellLearningPoints < slpCost)
                    {
                        ShowMessage(DataNameProvider.NotEnoughSpellLearningPoints);
                    }
                    else
                    {
                        CurrentPartyMember.SpellLearningPoints -= (ushort)slpCost;

                        if (RollDice100() < CurrentPartyMember.Skills[Skill.ReadMagic].TotalCurrentValue)
                        {
                            // Learned spell
                            ShowMessage(DataNameProvider.ManagedToLearnSpell, () =>
                            {
                                CurrentPartyMember.AddSpell(item.Spell);
                                layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50), true);
                            });
                        }
                        else
                        {
                            // Failed to learn the spell
                            ShowMessage(DataNameProvider.FailedToLearnSpell, () =>
                            {
                                layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50));
                            });
                        }
                    }
                }
            };

            PlayerSwitched();
        });
    }

    void AddExperience(List<PartyMember> partyMembers, uint amount, Action? finishedEvent = null)
    {
        void Add(int index)
        {
            if (index == partyMembers.Count)
            {
                finishedEvent?.Invoke();
                return;
            }

            AddExperience(partyMembers[index], amount, () => Add(index + 1));
        }

        Add(0);
    }

    public void AddExperience(PartyMember partyMember, uint amount, Action finishedEvent)
    {
        if (partyMember.AddExperiencePoints(amount, RandomInt, Features))
        {
            // Level-up
            ShowLevelUpWindow(partyMember, finishedEvent);
        }
        else
        {
            finishedEvent?.Invoke();
        }
    }

    void ShowLevelUpWindow(PartyMember partyMember, Action finishedEvent)
    {
        bool allInputWasDisabled = allInputDisabled;
        bool inputWasEnabled = InputEnable;
        InputEnable = false;
        allInputDisabled = false;
        CursorType = CursorType.Click;
        var lastPlayedSong = this.lastPlayedSong;
        var previousSong = PlayMusic(Song.StairwayToLevel50);
        var popup = layout.OpenPopup(new Position(16, 62), 18, 6);
        bool magicClass = partyMember.Class.IsMagic();

        void AddValueText<T>(int y, string text, T value, T? maxValue = null, string unit = "") where T : struct
        {
            popup.AddText(new Position(32, y), text, TextColor.BrightGray);
            popup.AddText(new Position(212, y), maxValue == null ? $"{value}{unit}" : $"{value}/{maxValue}{unit}", TextColor.BrightGray);
        }

        popup.AddText(new Rect(32, 78, 256, Global.GlyphLineHeight), partyMember.Name + string.Format(DataNameProvider.HasReachedLevel, partyMember.Level),
            TextColor.BrightGray, TextAlign.Center);

        AddValueText(92, DataNameProvider.LPAreNow, partyMember.HitPoints.CurrentValue, partyMember.HitPoints.MaxValue);
        if (magicClass)
        {
            AddValueText(99, DataNameProvider.SPAreNow, partyMember.SpellPoints.CurrentValue, partyMember.SpellPoints.MaxValue);
            AddValueText(106, DataNameProvider.SLPAreNow, partyMember.SpellLearningPoints);
        }
        AddValueText(113, DataNameProvider.TPAreNow, partyMember.TrainingPoints);
        AddValueText(120, DataNameProvider.APRAreNow, partyMember.AttacksPerRound);

        if (partyMember.Class < Class.Animal)
        {
            if (partyMember.Level >= 50)
                popup.AddText(new Position(32, 134), DataNameProvider.MaxLevelReached, TextColor.BrightGray);
            else
                AddValueText(134, DataNameProvider.NextLevelAt, partyMember.GetNextLevelExperiencePoints(Features), null, " " + DataNameProvider.EP);
        }

        popup.Closed += () =>
        {
            InputEnable = inputWasEnabled;
            allInputDisabled = allInputWasDisabled;
            PlayMusic(previousSong);
            this.lastPlayedSong = lastPlayedSong;
            finishedEvent?.Invoke();
        };
    }
}

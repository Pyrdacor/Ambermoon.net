﻿using Ambermoon.Data.Enumerations;
using System.ComponentModel;

namespace Ambermoon
{
    public enum PlayerMovementAbility
    {
        NoMovement,
        Walking,
        Swimming,
        FlyingDisc,
        Rafting, // raft
        Sailing, // boat
        WitchBroom,
        Eagle
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class PlayerMovementAbilityExtensions
    {
        public static PlayerMovementAbility ToPlayerMovementAbility(this TravelType travelType) => travelType switch
        {
            TravelType.Walk => PlayerMovementAbility.Walking,
            TravelType.Horse => PlayerMovementAbility.Walking,
            TravelType.SandLizard => PlayerMovementAbility.Walking, // TODO
            TravelType.Swim => PlayerMovementAbility.Swimming,
            TravelType.MagicalDisc => PlayerMovementAbility.FlyingDisc,
            TravelType.SandShip => PlayerMovementAbility.FlyingDisc, // TODO
            TravelType.Raft => PlayerMovementAbility.Rafting,
            TravelType.Ship => PlayerMovementAbility.Sailing,
            TravelType.WitchBroom => PlayerMovementAbility.WitchBroom,
            TravelType.Fly => PlayerMovementAbility.WitchBroom, // TODO
            TravelType.Eagle => PlayerMovementAbility.Eagle,
            _ => PlayerMovementAbility.NoMovement
        };
    }
}

﻿namespace Ambermoon.Render
{
    internal interface IMapCharacter
    {
        bool Interact(EventTrigger trigger, bool bed);
        Position Position { get; }
        bool CheckDeactivation(uint deactivatedEventIndex);
        void ResetLastInteractionTime();
    }
}

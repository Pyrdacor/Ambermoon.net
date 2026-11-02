using System;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.UI;

namespace Ambermoon;

partial class GameCore
{
    internal const uint WorldMapItemIndex = 160;
    internal const uint RuneAlphabetItemIndex = 145;
    WorldMapViewer? worldMapViewer = null;
    bool hadWorldMapItem = false;

    /// <summary>
    /// These items are not marked as important in the game data and can be
    /// sold or stored but they are too useful to get lost by dropping them.
    /// </summary>
    internal static bool CanDropItem(uint itemIndex) => itemIndex != WorldMapItemIndex && itemIndex != RuneAlphabetItemIndex;

    internal bool WorldMapOpen => worldMapViewer != null;

    /// <summary>
    /// Any party member owns the map of Lyramion.
    /// </summary>
    internal bool HasWorldMapItem => CurrentSavegame != null &&
        PartyMembers.Any(partyMember => partyMember.Inventory.Slots.Any(slot => !slot.Empty && slot.ItemIndex == WorldMapItemIndex));

    // Items might also be added or removed by events without the layout being updated.
    void UpdateWorldMapButton()
    {
        if (!Ingame || is3D || layout.Type != LayoutType.Map2D)
            return;

        bool hasItem = HasWorldMapItem;

        if (hasItem != hadWorldMapItem)
        {
            hadWorldMapItem = hasItem;
            layout.UpdateLayoutButtons();
        }
    }

    /// <summary>
    /// Shows the world map. The game is paused while it is open and
    /// the previous pause and input state is restored when it is closed.
    /// </summary>
    internal void ShowWorldMap(Action? closeAction = null)
    {
        if (worldMapViewer != null)
            return;

        bool wasPaused = paused;
        bool wasInputEnabled = InputEnable;
        var previousCursorType = cursor.Type;

        Pause();
        InputEnable = false;

        worldMapViewer = new WorldMapViewer(renderView, GameLanguage, CoreConfiguration.IsMobile);
        UpdateWorldMapCursor(lastMousePosition);
        worldMapViewer.Closed += () =>
        {
            worldMapViewer = null;

            if (!wasPaused)
                Resume();

            InputEnable = wasInputEnabled;
            cursor.Type = previousCursorType;
            ResetCursor();
            closeAction?.Invoke();
        };
    }

    // The cursor type is set directly as the CursorType property would trap the mouse on the map view.
    void UpdateWorldMapCursor(Position position)
    {
        lock (cursor)
        {
            cursor.Type = CursorType.Hand;
            cursor.UpdatePosition(position, this);
        }
    }

    public void OnPinchZoom(float factor, Position focus) => worldMapViewer?.OnPinchZoom(factor, focus);
}

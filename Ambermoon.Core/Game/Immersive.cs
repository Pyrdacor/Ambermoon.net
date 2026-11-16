using Ambermoon.Data.Enumerations;
using Ambermoon.UI;

namespace Ambermoon;

partial class GameCore
{
    bool immersiveMode = false;

    /// <summary>
    /// The immersive mode can be toggled on maps with the key I or the icon at the upper right.
    /// The map then covers the whole screen and only the names of the party members stay.
    /// </summary>
    public void ToggleImmersiveMode()
    {
        if (!Ingame || WindowActive || layout.PopupActive || layout.OptionMenuOpen || !InputEnable ||
            allInputDisabled || BattleActive || layout.IsDragging || CursorType > Data.CursorType.Wait ||
            (layout.Type != LayoutType.Map2D && layout.Type != LayoutType.Map3D))
            return;

        immersiveMode = !immersiveMode;
        layout.SetImmersive(immersiveMode);
        ApplyImmersiveMode();
    }

    /// <summary>
    /// True if a position is on a visible UI element of the immersive mode (not on the map).
    /// </summary>
    bool IsOnImmersiveUI(Position position)
    {
        if (layout.ImmersiveUIContains(position))
            return true;

        return layout.ImmersiveActive && QuestLogIconVisible && QuestLogIconArea.Contains(position);
    }

    /// <summary>
    /// Applies the immersive mode to the map view. This is called when the
    /// mode is toggled and whenever the map view is (re)created.
    /// </summary>
    void ApplyImmersiveMode()
    {
        if (!Ingame || Map == null)
            return;

        layout.SetImmersive(immersiveMode, false);

        bool active = layout.ImmersiveActive;

        if (mapViewRightFillArea != null)
            mapViewRightFillArea.Visible = !active;

        if (currentWindow.Window == Window.MapView)
            ShowWindowTitle(!active);

        if (is3D)
        {
            bool changed = Global.Map3DViewWidth != (active ? Global.VirtualScreenWidth : Global.DefaultMap3DViewWidth);
            Global.SetMap3DView(active);
            mapViewArea = Map3DViewArea;

            if (changed && renderMap3D != null)
            {
                renderMap3D.RefreshView();
                UpdateLight(); // recreates the sky
            }
        }
        else
        {
            Global.SetMap2DView(active, Map);
            mapViewArea = Map2DViewArea;

            if (renderMap2D != null && player != null)
                renderMap2D.RefreshView(player.Position);

            player2D?.RefreshView();
            ouchSprite.ClipArea = Map2DViewArea;

            if (fow2D != null)
            {
                fow2D.Resize(Global.Map2DViewWidth, Global.Map2DViewHeight);
                fow2D.X = Global.Map2DViewX;
                fow2D.Y = Global.Map2DViewY;

                if (player2D != null)
                    fow2D.Center = player2D.DisplayArea.Center;
            }
        }
    }
}

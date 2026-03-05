using System;
using System.Linq;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;

namespace Ambermoon;

partial class GameCore
{
    const int RuneTableItemIndex = 145;

    readonly string gameVersionName;
    readonly string fullVersion;

    internal string GameVersionName => gameVersionName;
    internal GameLanguage GameLanguage { get; private set; }
    public ICoreConfiguration CoreConfiguration { get; private set; }
    public bool AutoDerune => TravelType == TravelType.Fly || // Superman mode can also read runes as text
        (CoreConfiguration.AutoDerune && PartyMembers.Any(p => p.HasItem(RuneTableItemIndex)));

    public delegate void FullscreenChangeHandler(WindowMode windowMode);
    public delegate void ResolutionChangeHandler(int? oldWidth);

    readonly FullscreenChangeHandler fullscreenChangeHandler;
    readonly ResolutionChangeHandler resolutionChangeHandler;

    public event Action<ICoreConfiguration, bool>? ConfigurationChanged;

    internal void NotifyConfigurationChange(bool windowChange)
    {
        if (Is3D)
        {
            renderMap3D?.UpdateFloorAndCeilingVisibility(CoreConfiguration.ShowFloor, CoreConfiguration.ShowCeiling);
            renderMap3D?.SetFog(Map!, MapManager.GetLabdataForMap(Map));
        }

        ConfigurationChanged?.Invoke(CoreConfiguration, windowChange);

        // Ensure the music is updated
        UpdateMusic();

        if (windowChange && !Trapped)
        {
            trappedMousePositionOffset.X = 0;
            trappedMousePositionOffset.Y = 0;
            MouseTrappedChanged?.Invoke(false, lastMousePosition);
            UpdateCursor();
        }
    }
    internal void RequestFullscreenChange(WindowMode windowMode) => fullscreenChangeHandler?.Invoke(windowMode);
    internal void NotifyResolutionChange(int? oldWidth) => resolutionChangeHandler?.Invoke(oldWidth);
    public string GetFullVersion() => fullVersion;
    public void ExternalGraphicFilterChanged() => layout.OnExternalGraphicFilterChanged();
    public void ExternalGraphicFilterOverlayChanged() => layout.OnExternalGraphicFilterOverlayChanged();
    public void ExternalEffectsChanged() => layout.OnExternalEffectsChanged();
    public void ExternalBattleSpeedChanged()
    {
        SetBattleSpeed(CoreConfiguration.BattleSpeed);
        layout.OnExternalBattleSpeedChanged();
    }
    public void ExternalMusicChanged() => layout.OnExternalMusicChanged();
    public void ExternalVolumeChanged() => layout.OnExternalVolumeChanged();
    public void PreFullscreenChanged()
    {
        preFullscreenMousePosition = renderView.ScreenToGame(GetMousePosition(lastMousePosition));
        preFullscreenChangeTrapMouseArea = trapMouseGameArea;
        if (trapMouseGameArea != null)
            UntrapMouse();
    }
    public void PostFullscreenChanged()
    {
        if (preFullscreenMousePosition != null)
            lastMousePosition = renderView.GameToScreen(preFullscreenMousePosition);

        if (preFullscreenChangeTrapMouseArea != null)
        {
            TrapMouse(preFullscreenChangeTrapMouseArea);
            preFullscreenChangeTrapMouseArea = null;
        }
        else
        {
            MousePositionChanged?.Invoke(lastMousePosition);
        }

        if (layout?.OptionMenuOpen == true)
        {
            layout.UpdateFullscreenOption();
        }
    }
}

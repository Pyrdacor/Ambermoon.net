using System;
using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;

namespace Ambermoon;

partial class GameCore
{
    readonly ISongManager songManager;
    ISong? currentSong = null;
    Song? lastPlayedSong = null;
    bool disableMusicChange = false;

    internal IAudioOutput AudioOutput { get; private set; }

    /// <summary>
    /// Starts playing a specific music. If Song.Default is given
    /// the current map music is played instead.
    /// 
    /// Returns the previously played song.
    /// </summary>
    internal Song PlayMusic(Song song)
    {
        var lastSong = lastPlayedSong;
        lastPlayedSong = null;

        if (disableMusicChange)
            return currentSong?.Song ?? Song.Default;

        if (song == Song.Default || (int)song == 255)
        {
            if (Map!.UseTravelMusic)
            {
                var travelSong = TravelType.TravelSong();

                if (travelSong == Song.Default)
                    travelSong = Song.PloddingAlong;

                return PlayMusic(travelSong);
            }

            return PlayMusic(Map.MusicIndex == 0 || Map.MusicIndex == 255 ? (lastSong ?? Song.PloddingAlong) : (Song)Map.MusicIndex);
        }

        var newSong = songManager.GetSong(song);
        var oldSong = currentSong?.Song ?? Song.Default;

        if (currentSong != newSong)
        {
            currentSong?.Stop();
            currentSong = newSong;
            ContinueMusic();
        }

        return oldSong;
    }

    /// <summary>
    /// Starts playing the map's music.
    /// </summary>
    void PlayMapMusic() => PlayMusic(Song.Default);

    internal void EnableMusicChange(bool enable) => disableMusicChange = !enable;

    internal TimeSpan? GetCurrentSongDuration() => currentSong?.SongDuration;

    public void ContinueMusic()
    {
        if (CoreConfiguration.Music)
            currentSong?.Play(AudioOutput);
    }

    internal void UpdateMusic()
    {
        if (CoreConfiguration.Music && currentSong != null)
            PlayMusic(currentSong.Song);
    }

    // Elf harp
    void OpenMusicList(Action? finishAction = null)
    {
        bool wasPaused = paused;
        Pause();
        const int columns = 15;
        const int rows = 10;
        var popupArea = new Rect(16, 35, columns * 16, rows * 16);
        TrapMouse(new Rect(popupArea.Left + 16, popupArea.Top + 16, popupArea.Width - 32, popupArea.Height - 32));
        var popup = layout.OpenPopup(popupArea.Position, columns, rows, true, false);
        var songList = popup.AddSongListBox(Enumerable.Range(0, 32).Select(index => new KeyValuePair<string, Action<int, string>?>
        (
            DataNameProvider.GetSongName((Song)(index + 1)), PlaySong
        )).ToList());
        void PlaySong(int index, string name)
        {
            if (AudioOutput.Available)
            {
                AudioOutput.Enabled = CoreConfiguration.Music = true;
                PlayMusic((Song)(index + 1));
            }
        }
        var exitButton = popup.AddButton(new Position(190, 166));
        exitButton.ButtonType = ButtonType.Exit;
        exitButton.Disabled = false;
        exitButton.LeftClickAction = () => ClosePopup();
        exitButton.Visible = true;
        popup.Closed += () =>
        {
            UntrapMouse();
            if (!wasPaused)
                Resume();
            finishAction?.Invoke();
        };
        int scrollRange = Math.Max(0, 16); // = 32 songs - 16 songs visible
        var scrollbar = popup.AddScrollbar(layout, scrollRange, 2);
        scrollbar.Scrolled += offset =>
        {
            songList.ScrollTo(offset);
        };
    }
}

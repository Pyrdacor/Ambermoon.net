using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.ExecutableData;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ambermoon
{
    internal class Patcher
    {
        const string RecentInfoUrl = "https://ambermoon-net.pyrdacor.net/download/recent.txt";
        const string PatchBaseUrl = "https://ambermoon-net.pyrdacor.net/download/";

        readonly IRenderView renderView;
        string patchUrl;
        IColoredRect background = null;
        readonly List<ILayerSprite> borders = new List<ILayerSprite>();
        readonly List<UI.UIText> texts = new List<UI.UIText>();
        Render.Cursor cursor = null;
        readonly static Size windowSize = new Size(16, 8);
        readonly static Rect windowArea = new Rect
        (
            (Global.VirtualScreenWidth - windowSize.Width * 16) / 2,
            (Global.VirtualScreenHeight - windowSize.Height * 16) / 2 - 8,
            windowSize.Width * 16,
            windowSize.Height * 16
        );
        readonly static Rect clientArea = new Rect(windowArea.X + 16, windowArea.Y + 16, windowArea.Width - 32, windowArea.Height - 32);
        GameLanguage language = GameLanguage.English;
        readonly List<UI.Button> buttons = new List<UI.Button>();
        readonly List<IColoredRect> buttonBackgrounds = new List<IColoredRect>();
        readonly List<IColoredRect> filledAreas = new List<IColoredRect>();
        Action clickHandler = null;
        long ticks = 0;
        readonly object uiLock = new();
        BinaryReader patcherReader;
        readonly TextureAtlasManager textureAtlasManager;
        bool finished = false;
        readonly Queue<Action> uiChanges = new Queue<Action>();

        public Patcher(IRenderView renderView, BinaryReader patcherReader, TextureAtlasManager textureAtlasManager)
        {
            this.renderView = renderView;
            this.patcherReader = patcherReader;
            this.textureAtlasManager = textureAtlasManager;
        }

        void CleanUpTextsAndButtons(bool locked = false)
        {
            if (!locked)
                Monitor.Enter(uiLock);

            try
            {
                foreach (var text in texts)
                    text?.Destroy();

                texts.Clear();

                foreach (var button in buttons)
                    button?.Destroy();

                buttons.Clear();

                foreach (var buttonBackground in buttonBackgrounds)
                    buttonBackground?.Delete();

                buttonBackgrounds.Clear();

                foreach (var filledArea in filledAreas)
                    filledArea?.Delete();

                filledAreas.Clear();
            }
            finally
            {
                if (!locked)
                    Monitor.Exit(uiLock);
            }
        }

        void EnsureWindow()
        {
            CleanUpTextsAndButtons();

            if (background != null)
                return;

            var culture = CultureInfo.DefaultThreadCurrentCulture ?? CultureInfo.CurrentCulture;
            var cultureName = culture?.Name ?? "";
            language = cultureName == "de" || cultureName.StartsWith("de-") ? GameLanguage.German : GameLanguage.English;
            var textureAtlas = textureAtlasManager.GetOrCreate(Layer.UI);
            var fontTextureAtlas = textureAtlasManager.GetOrCreate(Layer.Text);
            var spriteFactory = renderView.SpriteFactory;
            var layer = renderView.GetLayer(Layer.UI);
            var executableData = ExecutableData.FromGameData(renderView.GameData);
            cursor = new Render.Cursor(renderView, executableData.Cursors.Entries.Select(c => new Position(c.HotspotX, c.HotspotY)).ToList().AsReadOnly(),
                textureAtlasManager);

            #region Window
            void AddBorder(UIGraphic frame, int column, int row)
            {
                var sprite = spriteFactory.Create(16, 16, true) as ILayerSprite;
                sprite.Layer = layer;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(274u + (uint)frame);
                sprite.PaletteIndex = 0;
                sprite.X = windowArea.X + column * 16;
                sprite.Y = windowArea.Y + row * 16;
                sprite.Visible = true;
                borders.Add(sprite);
            }
            // 4 corners
            AddBorder(UIGraphic.FrameUpperLeft, 0, 0);
            AddBorder(UIGraphic.FrameUpperRight, windowSize.Width - 1, 0);
            AddBorder(UIGraphic.FrameLowerLeft, 0, windowSize.Height - 1);
            AddBorder(UIGraphic.FrameLowerRight, windowSize.Width - 1, windowSize.Height - 1);
            // top and bottom border
            for (int i = 0; i < windowSize.Width - 2; ++i)
            {
                AddBorder(UIGraphic.FrameTop, i + 1, 0);
                AddBorder(UIGraphic.FrameBottom, i + 1, windowSize.Height - 1);
            }
            // left and right border
            for (int i = 0; i < windowSize.Height - 2; ++i)
            {
                AddBorder(UIGraphic.FrameLeft, 0, i + 1);
                AddBorder(UIGraphic.FrameRight, windowSize.Width - 1, i + 1);
            }
            background = FillArea(new Rect(windowArea.X + 16, windowArea.Y + 16,
                windowSize.Width * 16 - 32, windowSize.Height * 16 - 32), GetPaletteColor(28), 0);
            #endregion
        }

        static readonly Dictionary<GameLanguage, string[]> Texts = new Dictionary<GameLanguage, string[]>
        {
            { GameLanguage.German, new string[]
                {
                    "Willst du den Patcher verwenden? Die Startzeit wird leicht erhöht und man benötigt Internetzugriff. Neue Versionen werden dann aber automatisch erkannt und können direkt installiert werden.",
                    "Falls du den Patcher später nutzen möchtest, kannst du ihn in der Konfigurationsdatei 'ambermoon.cfg' manuell aktivieren.",
                    "Weiter per Klick",
                    "Eine neue Version ist verfügbar! Möchtest du sie jetzt herunterladen und installieren?",
                    "Die neue Version wurde nicht gefunden. Bitte melde das an Pyrdacor (trobt(at)web.de).",
                    "Fehler beim Herunterladen der neuen Version. Versuche es später nochmal oder lade sie dir manuell herunter.",
                    "Ambermoon {0} wird heruntergeladen ...",
                    "{0} von {1}",
                    "Fertig",
                    "Download abbrechen"
                }
            },
            { GameLanguage.English, new string[]
                {
                    "Do you want to use the patcher? It slightly increases startup time and you need internet access. But new versions will then be detected automatically and can be installed directly.",
                    "If you decide to use the patcher later, you can activate it in the config file 'ambermoon.cfg' manually.",
                    "Click to continue",
                    "A new version is available! Do you want to download and install it now?",
                    "The new version was not found. Please report this to Pyrdacor (trobt(at)web.de).",
                    "Failed to download the new version. Please try again later or download it manually.",
                    "Downloading Ambermoon {0} ...",
                    "{0} of {1}",
                    "Done",
                    "Cancel download"
                }
            }
        };

        enum TextId
        {
            WantToUsePatcher,
            UsePatcherLater,
            ContinueWithClick,
            NewVersionAvailable,
            VersionNotFound,
            FailedToDownload,
            Downloading,
            Progress,
            Done,
            AbortButton
        }

        string GetText(TextId index)
        {
            return Texts[language][(int)index];
        }

        public void AskToUsePatcher(Action patchAction, Action cleanUpAction)
        {
            EnsureWindow();

            AddText("Patcher", new Rect(clientArea.X, clientArea.Y + 4, clientArea.Width, 7), Data.Enumerations.Color.Yellow, TextAlign.Center);
            AddText(GetText(TextId.WantToUsePatcher), new Rect(clientArea.X + 4, clientArea.Y + 18, clientArea.Width - 8, 42), Data.Enumerations.Color.White);
            AddButton(ButtonType.Yes, clientArea.X + clientArea.Width / 2 - 40, clientArea.Y + 76, () =>
            {
                clickHandler = null;
                CleanUpTextsAndButtons();
                patchAction?.Invoke();
            });
            AddButton(ButtonType.No, clientArea.X + clientArea.Width / 2 + 8, clientArea.Y + 76, () =>
            {
                finished = true;
                CleanUpTextsAndButtons();
                AddText(GetText(TextId.UsePatcherLater), new Rect(clientArea.X + 4, clientArea.Y + 18, clientArea.Width - 8, 42), Data.Enumerations.Color.White);
                AddText(GetText(TextId.ContinueWithClick), new Rect(clientArea.X, clientArea.Y + 64, clientArea.Width, 7), Data.Enumerations.Color.White, TextAlign.Center);
                clickHandler = cleanUpAction;
            });
        }

        UI.Button AddButton(ButtonType buttonType, int x, int y, Action buttonAction, byte displayLayer = 20, bool locked = false)
        {
            if (!locked)
                Monitor.Enter(uiLock);

            try
            {
                buttonBackgrounds.Add(FillArea(new Rect(x, y, UI.Button.Width, UI.Button.Height), Ambermoon.Render.Color.Black, displayLayer, true));
                var button = new UI.Button(renderView, new Position(x, y), textureAtlasManager);
                button.ButtonType = buttonType;
                button.DisplayLayer = (byte)(displayLayer + 1);
                button.LeftClickAction = buttonAction;
                button.Visible = true;
                buttons.Add(button);
                return button;
            }
            finally
            {
                if (!locked)
                    Monitor.Exit(uiLock);
            }
        }

        public void CheckPatches(Action<Func<bool>> closeAppAction, Action noPatchAction, ref int timeout)
        {
            string version;
            using var httpClient = new HttpClient();
            Assembly assembly;

            try
            {
                assembly = Assembly.GetEntryAssembly();

                httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);
                var response = httpClient.GetAsync(RecentInfoUrl).Result;

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    noPatchAction?.Invoke();
                    return;
                }

                version = response.Content.ReadAsStringAsync().Result;
                var versionRegex = new Regex(@"^([0-9]+)\.([0-9]+)\.([0-9]+)$");
                Match match = null;

                if (version == null || !(match = versionRegex.Match(version)).Success)
                {
                    finished = true;
                    noPatchAction?.Invoke();
                    return;
                }

                int major = int.Parse(match.Groups[1].Value);
                int minor = int.Parse(match.Groups[2].Value);
                int patch = int.Parse(match.Groups[3].Value);
                var assemblyVersion = assembly.GetName().Version;

                if (assemblyVersion.Major > major)
                {
                    finished = true;
                    noPatchAction?.Invoke();
                    return;
                }

                if (assemblyVersion.Major == major)
                {
                    if (assemblyVersion.Minor > minor)
                    {
                        finished = true;
                        noPatchAction?.Invoke();
                        return;
                    }

                    if (assemblyVersion.Minor == minor && assemblyVersion.Build >= patch)
                    {
                        finished = true;
                        noPatchAction?.Invoke();
                        return;
                    }
                }

                patchUrl = PatchBaseUrl + version + "/" + GetPatchFileName();
            }
            catch (Exception ex)
            {
                finished = true;

                if (ex is TaskCanceledException || (ex is AggregateException aex && aex.InnerException is TaskCanceledException))
                {
                    if (timeout < 2000)
                        timeout = Math.Min(2000, timeout + 250);

                    Console.WriteLine("Server connection timed out. No check for patches possible.");
                }
                else
                {
                    Console.WriteLine("Error determining available patches: " + ex.ToString());
                }

                noPatchAction?.Invoke();
                return;
            }

            EnsureWindow();

            var textArea = new Rect(clientArea.X + 4, clientArea.Y + 18, clientArea.Width - 8, 42);

            AddText($"Ambermoon.net {version}", new Rect(clientArea.X, clientArea.Y + 4, clientArea.Width, 7), Data.Enumerations.Color.Yellow, TextAlign.Center);
            AddText(GetText(TextId.NewVersionAvailable), textArea, Data.Enumerations.Color.White);
            AddButton(ButtonType.Yes, clientArea.X + clientArea.Width / 2 - 40, clientArea.Y + 76, () =>
            {
                CleanUpTextsAndButtons();
                AddText(string.Format(GetText(TextId.Downloading), version), new Rect(clientArea.X, clientArea.Y + 4, clientArea.Width, 7), Data.Enumerations.Color.LightGreen, TextAlign.Center);
                AddSunkenBox(new Rect(clientArea.X + 3, clientArea.Y + 31, clientArea.Width - 6, 18));
                var textProgressInBytes = AddText("", new Rect(clientArea.X + 54, clientArea.Y + 32 + 20, clientArea.Width - 58, 7), Data.Enumerations.Color.White, TextAlign.Right);
                var textProgress = AddText("0%", new Rect(clientArea.X + 4, clientArea.Y + 32 + 20, 50, 7), Data.Enumerations.Color.White, TextAlign.Left, 51);
                var progressBar = FillArea(new Rect(clientArea.X + 4, clientArea.Y + 32, 0, 16), Ambermoon.Render.Color.Green, 2);
                filledAreas.Add(progressBar);
                var cancellationTokenSource = new CancellationTokenSource();

                static string MBString(long bytes)
                {
                    return $"{bytes / (1024 * 1024)}.{Util.Floor((float)(bytes % (1024 * 1024)) / (1024 * 1024) * 10.0f)} MB";
                }

                try
                {
                    var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromMinutes(60); // limit to 1h download time

                    void SafeDisposeClient()
                    {
                        try
                        {
                            httpClient?.Dispose();
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    long? totalSize = null;
                    var memoryStream = new MemoryStream();
                    var progress = new Progress<float>(progress =>
                    {
                        if (progress < 0.0f) // error
                        {
                            finished = true;
                            SafeDisposeClient();
                            clickHandler = noPatchAction;
                            lock (uiLock)
                            {
                                uiChanges.Enqueue(() =>
                                {
                                    CleanUpTextsAndButtons(true);
                                    AddText(GetText(TextId.FailedToDownload),
                                        textArea, Data.Enumerations.Color.LightRed, TextAlign.Left, 50, true);
                                    AddText(GetText(TextId.ContinueWithClick), new Rect(clientArea.X, clientArea.Y + 64, clientArea.Width, 7),
                                        Data.Enumerations.Color.White, TextAlign.Center, 50, true);
                                });
                            }
                            return;
                        }

                        lock (uiLock)
                        {
                            uiChanges.Enqueue(() =>
                            {
                                progressBar.Resize(Util.Round((clientArea.Width - 8) * progress), progressBar.Height);
                            });
                        }

                        if (progress >= 1.0f)
                        {
                            finished = true;
                            SafeDisposeClient();

                            if (cancellationTokenSource.IsCancellationRequested)
                            {
                                noPatchAction?.Invoke();
                                return;
                            }

                            lock (uiLock)
                            {
                                uiChanges.Enqueue(() =>
                                {
                                    textProgress.SetText(renderView.TextProcessor.CreateText(GetText(TextId.Done)));
                                    string totalSizeString = MBString(totalSize.Value);
                                    textProgressInBytes.SetText(renderView.TextProcessor.CreateText(string.Format(GetText(TextId.Progress), totalSizeString, totalSizeString)));
                                });
                            }

                            var tempPath = Path.GetTempFileName();
                            using var fileStream = File.OpenWrite(tempPath);
                            memoryStream.Position = 0;
                            memoryStream.CopyTo(fileStream);

                            lock (uiLock)
                            {
                                uiChanges.Enqueue(() =>
                                {
                                    buttons.ForEach(b => b?.Destroy());
                                    buttons.Clear();
                                    buttonBackgrounds.ForEach(b => b?.Delete());
                                    buttonBackgrounds.Clear();
                                    AddText(GetText(TextId.ContinueWithClick), new Rect(clientArea.X, clientArea.Y + 78, clientArea.Width, 7),
                                        Data.Enumerations.Color.White, TextAlign.Center, 50, true);
                                });
                            }

                            clickHandler = () =>
                            {
                                closeAppAction(() => WriteAndRunInstaller(tempPath));
                            };
                        }
                        else if (!finished)
                        {
                            lock (uiLock)
                            {
                                uiChanges.Enqueue(() =>
                                {
                                    if (buttons.Count == 0)
                                    {
                                        var button = AddButton(ButtonType.DisarmTrap, clientArea.X + clientArea.Width / 2 - UI.Button.Width / 2, clientArea.Y + 78, () =>
                                        {
                                            cancellationTokenSource.Cancel();
                                        }, 20, true);
                                        button.Tooltip = GetText(TextId.AbortButton);
                                        button.TooltipColor = Data.Enumerations.Color.LightRed;
                                        button.TooltipOffset = new Position(0, -4);
                                    }

                                    textProgress.SetText(renderView.TextProcessor.CreateText($"{Util.Round(progress * 100.0f)}%"));
                                });
                            }
                        }
                    });
                    var progressInBytes = new Progress<long>(bytes =>
                    {
                        if (totalSize != null && totalSize > 0)
                        {
                            string sizeString = bytes switch
                            {
                                < 1024 => $"{bytes} B",
                                < 1024 * 1024 => $"{bytes / 1024} KB",
                                _ => MBString(bytes)
                            };

                            lock (uiLock)
                            {
                                uiChanges.Enqueue(() =>
                                {
                                    textProgressInBytes.SetText(renderView.TextProcessor.CreateText(string.Format(GetText(TextId.Progress), sizeString, MBString(totalSize.Value))));
                                });
                            }
                        }
                    });
                    Task.Run(() => httpClient.DownloadAsync(patchUrl, memoryStream, progress, progressInBytes, size => totalSize = size, statusCode =>
                    {
                        if (statusCode != HttpStatusCode.OK)
                        {
                            finished = true;
                            SafeDisposeClient();
                            clickHandler = noPatchAction;
                            lock (uiLock)
                            {
                                uiChanges.Enqueue(() =>
                                {
                                    CleanUpTextsAndButtons(true);
                                    AddText(GetText(statusCode == HttpStatusCode.NotFound ? TextId.VersionNotFound : TextId.FailedToDownload),
                                        textArea, Data.Enumerations.Color.LightRed, TextAlign.Left, 50, true);
                                    AddText(GetText(TextId.ContinueWithClick), new Rect(clientArea.X, clientArea.Y + 64, clientArea.Width, 7),
                                        Data.Enumerations.Color.White, TextAlign.Center, 50, true);
                                });
                            }
                        }
                    }, cancellationTokenSource.Token));
                }
                catch (Exception ex)
                {
                    finished = true;

                    try
                    {
                        httpClient?.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }
                    CleanUpTextsAndButtons();
                    if (ex is TaskCanceledException && cancellationTokenSource.IsCancellationRequested)
                    {
                        noPatchAction?.Invoke();
                    }
                    else
                    {
                        AddText(GetText(TextId.FailedToDownload), textArea, Data.Enumerations.Color.LightRed);
                        AddText(GetText(TextId.ContinueWithClick), new Rect(clientArea.X, clientArea.Y + 64, clientArea.Width, 7), Data.Enumerations.Color.White, TextAlign.Center);
                        clickHandler = noPatchAction;
                    }
                }
            });
            AddButton(ButtonType.No, clientArea.X + clientArea.Width / 2 + 8, clientArea.Y + 76, () =>
            {
                finished = true;
                noPatchAction?.Invoke();
            });
        }

        Render.Color GetPaletteColor(byte colorIndex)
        {
            var paletteData = renderView.GraphicProvider.Palettes[50].Data;
            return new Render.Color
            (
                paletteData[colorIndex * 4 + 0],
                paletteData[colorIndex * 4 + 1],
                paletteData[colorIndex * 4 + 2],
                paletteData[colorIndex * 4 + 3]
            );
        }

        IColoredRect FillArea(Rect area, Render.Color color, byte displayLayer = 1, bool locked = false)
        {
            if (!locked)
                Monitor.Enter(uiLock);

            try
            {
                var filledArea = renderView.ColoredRectFactory.Create(area.Width, area.Height, color, displayLayer);
                filledArea.Layer = renderView.GetLayer(Layer.UI);
                filledArea.X = area.Left;
                filledArea.Y = area.Top;
                filledArea.Visible = true;
                return filledArea;
            }
            finally
            {
                if (!locked)
                    Monitor.Exit(uiLock);
            }
        }

        UI.UIText AddText(string text, Rect area, Data.Enumerations.Color color, TextAlign textAlign = TextAlign.Left, byte displayLayer = 50, bool locked = false)
        {
            if (!locked)
                Monitor.Enter(uiLock);

            try
            {
                var uiText = new UI.UIText(renderView, 49,
                    renderView.TextProcessor.WrapText(renderView.TextProcessor.CreateText(text, '?'), area, new Size(Global.GlyphWidth, Global.GlyphLineHeight)), area, displayLayer);
                uiText.SetTextColor(color);
                uiText.SetTextAlign(textAlign);
                uiText.Visible = true;
                texts.Add(uiText);
                return uiText;
            }
            finally
            {
                if (!locked)
                    Monitor.Exit(uiLock);
            }
        }

        void AddSunkenBox(Rect area, byte displayLayer = 1, byte fillColorIndex = 27)
        {
            lock (uiLock)
            {
                var darkBorderColor = GetPaletteColor(26);
                var brightBorderColor = GetPaletteColor(31);
                var fillColor = GetPaletteColor(fillColorIndex);

                // upper dark border
                filledAreas.Add(FillArea(new Rect(area.X, area.Y, area.Width - 1, 1), darkBorderColor, displayLayer));
                // left dark border
                filledAreas.Add(FillArea(new Rect(area.X, area.Y + 1, 1, area.Height - 2), darkBorderColor, displayLayer));
                // fill
                filledAreas.Add(FillArea(new Rect(area.X + 1, area.Y + 1, area.Width - 2, area.Height - 2), fillColor, displayLayer));
                // right bright border
                filledAreas.Add(FillArea(new Rect(area.Right - 1, area.Y + 1, 1, area.Height - 2), brightBorderColor, displayLayer));
                // lower bright border
                filledAreas.Add(FillArea(new Rect(area.X + 1, area.Bottom - 1, area.Width - 1, 1), brightBorderColor, displayLayer));
            }
        }

        public void CleanUp(bool cleanUpCursor)
        {
            clickHandler = null;
            background?.Delete();
            background = null;

            foreach (var border in borders)
                border?.Delete();

            borders.Clear();

            CleanUpTextsAndButtons();

            if (cleanUpCursor)
                cursor?.Destroy();

            patcherReader?.Dispose();
            patcherReader = null;
        }

        static string GetPatchFileName()
        {
            if (OperatingSystem.IsMacOS())
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm || RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                    return "Ambermoon.net-Mac-ARM.zip";
                else
                    return "Ambermoon.net-Mac.zip";
            }
            else if (OperatingSystem.IsWindows())
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                    return "Ambermoon.net-Windows32Bit.zip";
                else
                    return "Ambermoon.net-Windows.zip";
            }
            else
            {
                return "Ambermoon.net-Linux.tar.gz";
            }
        }

        public void OnMouseDown(Position position, MouseButtons buttons)
        {
            if (clickHandler != null)
            {
                clickHandler();
                clickHandler = null;
                return;
            }

            position = renderView.ScreenToGame(position);

            lock (uiLock)
            {
                foreach (var button in this.buttons.ToList())
                {
                    if (!button.Disabled && button.Area.Contains(position))
                    {
                        if (buttons == MouseButtons.Left)
                            button.LeftMouseDown(position, (uint)(ticks & uint.MaxValue));
                        else if (buttons == MouseButtons.Right)
                            button.RightMouseDown(position, (uint)(ticks & uint.MaxValue));
                        return;
                    }
                }
            }
        }

        public void OnMouseUp(Position position, MouseButtons buttons)
        {
            position = renderView.ScreenToGame(position);

            lock (uiLock)
            {
                foreach (var button in this.buttons.ToList())
                {
                    if (buttons == MouseButtons.Left)
                        button.LeftMouseUp(position, (uint)(ticks & uint.MaxValue));
                    else if (buttons == MouseButtons.Right)
                        button.RightMouseUp(position, (uint)(ticks & uint.MaxValue));
                }
            }
        }

        public void OnMouseMove(Position position, MouseButtons buttons)
        {
            lock (uiLock)
            {
                cursor.UpdatePosition(position, null);

                position = renderView.ScreenToGame(position);

                foreach (var button in this.buttons.ToList())
                    button.Hover(position);
            }
        }

        public void Update(double deltaTime)
        {
            lock (uiLock)
            {
                while (uiChanges.Count != 0)
                {
                    uiChanges.Dequeue()?.Invoke();
                }
            }

            ticks += Util.Round(Game.TicksPerSecond * (float)deltaTime);
        }

        public void Render()
        {
            lock (uiLock)
            {
                renderView.Render(new FloatPosition());
            }
        }

        bool WriteAndRunInstaller(string downloadPath)
        {
            try
            {
                var installDirectory = Configuration.ExecutableDirectoryPath;
                string patcherFile;

                if (!OperatingSystem.IsMacOS())
                {
                    patcherFile = Path.Combine(Path.GetTempPath(), "AmbermoonPatcher" + (OperatingSystem.IsWindows() ? ".exe" : ""));
                    File.WriteAllBytes(patcherFile, patcherReader.ReadBytes((int)patcherReader.BaseStream.Length));
                }
                else
                {
                    patcherFile = Path.Combine(Configuration.ExecutableDirectoryPath, "AmbermoonPatcher");
                }

                Process.Start(patcherFile, $"\"{downloadPath}\" \"{installDirectory}\"");
                return true;
            }
            catch
            {
                Console.WriteLine("Unable to write or start the patcher script. Please update the game manually.");
                return false;
            }
        }
    }
}

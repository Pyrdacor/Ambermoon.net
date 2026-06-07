using System.Text;
using Ambermoon.Frontend;
using Android.Graphics;
using Android.Views;
using Android.Views.InputMethods;

#nullable enable

namespace AmbermoonAndroid;

public class ConsoleOverlayManager : IConsole
{
    private static ConsoleOverlayManager? instance;
    public static ConsoleOverlayManager GetInstance(Activity activity)
        => instance ??= new ConsoleOverlayManager(activity);

    public event Action<string>? OnCommand;

    private readonly Activity activity;
    private FrameLayout? overlay;
    private TextView? outputView;
    private EditText? inputField;
    private ScrollView? scrollView;
    private Button? toggleButton;
    private readonly StringBuilder logLineBuilder = new();
    private readonly LinkedList<string> logLines = new();
    private const int MaxLines = 200;
    private bool initialized;
    private int cursorPosition = 0;
    private string inputText = "";

    private ConsoleOverlayManager(Activity activity)
    {
        this.activity = activity;
    }

    public void Initialize()
    {
        if (initialized) return;

        initialized = true;

        activity.RunOnUiThread(BuildOverlay);
    }

    public void Show() => activity.RunOnUiThread(() =>
    {
        if (overlay is null) return;
        overlay.Visibility = ViewStates.Visible;
        inputField?.RequestFocus();
        ShowKeyboard();
    });

    public void Hide() => activity.RunOnUiThread(() =>
    {
        if (overlay is null) return;
        overlay.Visibility = ViewStates.Gone;
        HideKeyboard();
    });

    public void Toggle()
    {
        if (overlay?.Visibility == ViewStates.Visible)
            Hide();
        else
            Show();
    }

    public bool IsVisible => overlay?.Visibility == ViewStates.Visible;

    public void WriteLine(string line)
    {
        activity.RunOnUiThread(() => AppendLine(line));
    }

    public void Write(string text)
    {
        activity.RunOnUiThread(() => AppendText(text));
    }

    public void WriteToInput(string text)
    {
        activity.RunOnUiThread(() =>
        {
            if (inputField is null) return;
            inputField.Text = text;
            SetCursorPosition(text.Length);
        });
    }

    public int CursorPosition
    {
        get => GetCursorPosition();
        set => SetCursorPosition(value);
    }

    private void SetCursorPosition(int position)
    {
        activity.RunOnUiThread(() =>
        {
            if (inputField is null) return;
            var length = inputField.Text?.Length ?? 0;
            var pos = Math.Clamp(position, 0, length);
            inputField.SetSelection(pos);
        });
    }

    private int GetCursorPosition()
    {
        if (inputField is null) return 0;
        var pos = inputField.SelectionStart;
        var length = inputField.Text?.Length ?? 0;
        return Math.Clamp(pos, 0, length);
    }

    public void Clear() => activity.RunOnUiThread(() =>
    {
        logLines.Clear();
        if (outputView != null)
            outputView.Text = string.Empty;
    });

    public void RemoveLastInput() => activity.RunOnUiThread(() =>
    {
        if (inputField is null) return;
        var text = inputField.Text?.ToString() ?? string.Empty;
        if (text.Length == 0) return;
        inputField.Text = text[..^1];
        SetCursorPosition(inputField.Text.Length);
    });

    private void BuildOverlay()
    {
        var displayMetrics = activity.Resources?.DisplayMetrics;
        int screenHeight = displayMetrics?.HeightPixels ?? 600;

        overlay = new FrameLayout(activity)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                screenHeight,
                GravityFlags.Bottom)
        };
        overlay.SetBackgroundColor(Color.Argb(210, 0, 0, 0));
        overlay.Visibility = ViewStates.Gone;

        var container = new LinearLayout(activity)
        {
            Orientation = Orientation.Vertical
        };
        container.SetPadding(12, 12, 12, 12);

        var title = new TextView(activity);
        title.Text = "  ▶ CHEAT CONSOLE";
        title.SetTextColor(Color.ParseColor("#00FF88"));
        title.SetTypeface(Typeface.Monospace, TypefaceStyle.Bold);
        title.TextSize = 10f;
        title.SetPadding(0, 0, 0, 6);
        container.AddView(title);

        var divider = new View(activity);
        divider.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 1);
        divider.SetBackgroundColor(Color.ParseColor("#00FF88"));
        container.AddView(divider);

        outputView = new TextView(activity);
        outputView.SetTextColor(Color.ParseColor("#CCFFCC"));
        outputView.SetTypeface(Typeface.Monospace, TypefaceStyle.Normal);
        outputView.TextSize = 10.5f;
        outputView.SetPadding(4, 6, 4, 6);

        scrollView = new ScrollView(activity);
        scrollView.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            0, 1f);
        scrollView.AddView(outputView);
        container.AddView(scrollView);

        var inputRow = new LinearLayout(activity)
        {
            Orientation = Orientation.Horizontal
        };
        var inputParams = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);
        inputParams.SetMargins(0, 8, 0, 0);
        inputRow.LayoutParameters = inputParams;

        var prompt = new TextView(activity);
        prompt.Text = ">";
        prompt.SetTextColor(Color.ParseColor("#00FF88"));
        prompt.SetTypeface(Typeface.Monospace, TypefaceStyle.Bold);
        prompt.TextSize = 13f;
        prompt.SetPadding(0, 0, 8, 0);
        prompt.Gravity = GravityFlags.CenterVertical;

        inputField = new EditText(activity);
        inputField.SetTextColor(Color.White);
        inputField.SetHintTextColor(Color.Gray);
        inputField.Hint = "Cheat-Code...";
        inputField.SetTypeface(Typeface.Monospace, TypefaceStyle.Normal);
        inputField.SetBackgroundColor(Color.Argb(255, 20, 20, 20));
        inputField.TextSize = 12f;
        inputField.SetPadding(16, 4, 16, 0);
        inputField.LayoutParameters = new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f);
        inputField.ImeOptions = ImeAction.Done;
        inputField.Focusable = false;
        inputField.ShowSoftInputOnFocus = false;
        inputField.SetSingleLine(true);

        // Enter → Absenden
        inputField.EditorAction += (_, e) =>
        {
            if (e.ActionId == ImeAction.Done ||
                e.ActionId == ImeAction.Go ||
                e.ActionId == ImeAction.Send)
            {
                SubmitInput();
                e.Handled = true;
            }
        };

        inputRow.AddView(prompt);
        inputRow.AddView(inputField);
        container.AddView(inputRow);

        overlay.AddView(container);

        var rootView = (ViewGroup?)activity.Window?
            .DecorView.FindViewById(Android.Resource.Id.Content);
        rootView?.AddView(overlay);

        BuildToggleButton();
        BuildMiniKeyboard(container);
    }

    private void SubmitInput()
    {
        var text = inputField?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return;

        AppendLine($"> {text}");
        inputField!.Text = string.Empty;
        cursorPosition = 0;

        OnCommand?.Invoke(text);
    }

    private void AppendText(string text)
    {
        if (logLineBuilder.Length == 0) // New line starting
        {
            logLineBuilder.Append(text);
            logLines.AddLast(text);
            while (logLines.Count > MaxLines)
                logLines.RemoveFirst();

            if (outputView is null) return;
            outputView.Text = string.Join("\n", logLines);

            // Auto-Scroll
            scrollView?.Post(() =>
                scrollView.FullScroll(FocusSearchDirection.Down));
        }
        else
        {
            logLineBuilder.Append(text);
            logLines.Last!.Value = text.ToString();

            if (outputView is null) return;
            outputView.Text = string.Join("\n", logLines);
        }        
    }

    private void AppendLine(string line)
    {
        if (logLineBuilder.Length > 0)
        {
            line = logLineBuilder.ToString() + line;
            logLineBuilder.Clear();

            logLines.Last!.Value = line;

            if (outputView is null) return;
            outputView.Text = string.Join("\n", logLines);
        }
        else
        {
            logLines.AddLast(line);
            while (logLines.Count > MaxLines)
                logLines.RemoveFirst();

            if (outputView is null) return;
            outputView.Text = string.Join("\n", logLines);

            // Auto-Scroll
            scrollView?.Post(() =>
                scrollView.FullScroll(FocusSearchDirection.Down));
        }
    }

    private void ShowKeyboard()
    {
        var imm = (InputMethodManager?)activity
            .GetSystemService(Android.Content.Context.InputMethodService);
        imm?.ShowSoftInput(inputField, ShowFlags.Implicit);
    }

    private void HideKeyboard()
    {
        var imm = (InputMethodManager?)activity
            .GetSystemService(Android.Content.Context.InputMethodService);
        var token = activity.CurrentFocus?.WindowToken;
        imm?.HideSoftInputFromWindow(token, 0);
    }

    private int DpToPx(int dp)
    {
        var density = activity.Resources?.DisplayMetrics?.Density ?? 1f;
        return (int)(dp * density);
    }

    private void BuildToggleButton()
    {
        toggleButton = new Button(activity);
        toggleButton.Text = "⌨";
        toggleButton.SetTextColor(Color.ParseColor("#00FF88"));
        toggleButton.SetBackgroundColor(Color.Argb(180, 0, 0, 0));
        toggleButton.TextSize = 16f;
        toggleButton.SetPadding(4, 4, 4, 4);
        toggleButton.Click += (_, _) => Toggle();
        toggleButton.Visibility = ViewStates.Gone;

        var parameters = new FrameLayout.LayoutParams(
            DpToPx(44),
            DpToPx(44),
            GravityFlags.Top | GravityFlags.Right);
        parameters.SetMargins(0, DpToPx(8), DpToPx(8), 0);
        toggleButton.LayoutParameters = parameters;

        var rootView = (ViewGroup?)activity.Window?
            .DecorView.FindViewById(global::Android.Resource.Id.Content);
        rootView?.AddView(toggleButton);
    }

    public void SetToggleButtonVisible(bool visible)
    {
        activity.RunOnUiThread(() =>
        {
            if (toggleButton is null) return;
            toggleButton.Visibility = visible ? ViewStates.Visible : ViewStates.Gone;
        });
    }

    private void BuildMiniKeyboard(LinearLayout container)
    {
        string[] rows =
        [
            "1234567890",
            "qwertyuiop",
            "asdfghjkl",
            "zxcvbnm"
        ];

        var keyboardWrapper = new LinearLayout(activity)
        {
            Orientation = Orientation.Vertical
        };
        keyboardWrapper.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);
        keyboardWrapper.SetPadding(0, 6, 0, 0);

        foreach (var row in rows)
        {
            var rowLayout = new LinearLayout(activity) { Orientation = Orientation.Horizontal };
            rowLayout.LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent);
            rowLayout.SetPadding(0, 2, 0, 2);

            foreach (var ch in row)
            {
                var key = BuildKey(
                    label: ch.ToString(),
                    weight: 1f,
                    bgColor: Color.Argb(255, 45, 45, 45),
                    onClick: () => { inputText = inputText.Insert(cursorPosition, ch.ToString()); cursorPosition++; UpdateInputDisplay(); }
                );
                rowLayout.AddView(key);
            }

            keyboardWrapper.AddView(rowLayout);
        }

        // Sonderzeichen-Zeile
        keyboardWrapper.AddView(BuildSpecialKeyRow());

        container.AddView(keyboardWrapper);
    }

    private LinearLayout BuildSpecialKeyRow()
    {
        var row = new LinearLayout(activity) { Orientation = Orientation.Horizontal };
        row.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);
        row.SetPadding(0, 2, 0, 2);

        // Left arrow
        row.AddView(BuildKey(
            label: "←",
            weight: 1.5f,
            bgColor: Color.Argb(255, 60, 60, 60),
            onClick: () =>
            {
                if (cursorPosition > 0)
                {
                    cursorPosition--;
                    UpdateInputDisplay();
                }
            }
        ));

        // Right arrow
        row.AddView(BuildKey(
            label: "→",
            weight: 1.5f,
            bgColor: Color.Argb(255, 60, 60, 60),
            onClick: () =>
            {
                if (cursorPosition < inputText.Length)
                {
                    cursorPosition++;
                    UpdateInputDisplay();
                }
            }
        ));

        // Space
        row.AddView(BuildKey(
            label: "SPACE",
            weight: 3f,
            bgColor: Color.Argb(255, 60, 60, 60),
            onClick: () => { inputText = inputText.Insert(cursorPosition, " "); cursorPosition++; UpdateInputDisplay(); }
        ));

        // Delete
        row.AddView(BuildKey(
            label: "DEL",
            weight: 1.5f,
            bgColor: Color.Argb(255, 100, 30, 30),
            onClick: () =>
            {
                if (inputText.Length > 0 && cursorPosition < inputText.Length - 1)
                {
                    inputText = inputText.Remove(cursorPosition, 1);
                    UpdateInputDisplay();
                }
            }
        ));

        // Backspace
        row.AddView(BuildKey(
            label: "⌫",
            weight: 1.5f,
            bgColor: Color.Argb(255, 80, 50, 30),
            onClick: () =>
            {
                if (inputText.Length > 0 && cursorPosition > 0)
                {
                    inputText = inputText.Remove(--cursorPosition, 1);
                    UpdateInputDisplay();
                }
            }
        ));

        // Enter
        row.AddView(BuildKey(
            label: "↵",
            weight: 1.5f,
            bgColor: Color.ParseColor("#00AA55"),
            onClick: SubmitInput
        ));

        return row;
    }

    private Button BuildKey(string label, float weight, Color bgColor, Action onClick)
    {
        var key = new Button(activity);
        key.Text = label;
        key.SetTextColor(Color.White);
        key.SetBackgroundColor(bgColor);
        key.TextSize = 10f;
        key.SetPadding(0, 0, 0, 0);

        var lp = new LinearLayout.LayoutParams(0, DpToPx(32), weight);
        lp.SetMargins(1, 1, 1, 1);
        key.LayoutParameters = lp;

        key.Click += (_, _) => onClick();
        return key;
    }

    private void UpdateInputDisplay()
    {
        if (inputField != null)
        {
            inputField.Text = inputText;
            inputField.SetSelection(Math.Clamp(cursorPosition, 0, inputText.Length));
        }
    }
}
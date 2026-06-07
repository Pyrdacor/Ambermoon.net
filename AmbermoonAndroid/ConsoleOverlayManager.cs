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
    private TextView? _outputView;
    private EditText? inputField;
    private ScrollView? scrollView;
    private readonly StringBuilder logLineBuilder = new();
    private readonly LinkedList<string> logLines = new();
    private const int MaxLines = 200;
    private bool initialized;

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
        if (_outputView != null)
            _outputView.Text = string.Empty;
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
        overlay = new FrameLayout(activity)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent,
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
        title.Text = "▶ CHEAT CONSOLE";
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

        _outputView = new TextView(activity);
        _outputView.SetTextColor(Color.ParseColor("#CCFFCC"));
        _outputView.SetTypeface(Typeface.Monospace, TypefaceStyle.Normal);
        _outputView.TextSize = 10.5f;
        _outputView.SetPadding(4, 6, 4, 6);

        scrollView = new ScrollView(activity);
        scrollView.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            DpToPx(180));
        scrollView.AddView(_outputView);
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
        inputField.SetPadding(8, 4, 8, 4);
        inputField.LayoutParameters = new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f);
        inputField.ImeOptions = ImeAction.Done;
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

        var sendButton = new Button(activity);
        sendButton.Text = "OK";
        sendButton.SetTextColor(Color.Black);
        sendButton.SetBackgroundColor(Color.ParseColor("#00FF88"));
        sendButton.TextSize = 11f;
        sendButton.SetPadding(16, 0, 16, 0);
        sendButton.Click += (_, _) => SubmitInput();

        inputRow.AddView(prompt);
        inputRow.AddView(inputField);
        inputRow.AddView(sendButton);
        container.AddView(inputRow);

        overlay.AddView(container);

        var rootView = (ViewGroup?)activity.Window?
            .DecorView.FindViewById(Android.Resource.Id.Content);
        rootView?.AddView(overlay);
    }

    private void SubmitInput()
    {
        var text = inputField?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return;

        AppendLine($"> {text}");
        inputField!.Text = string.Empty;

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

            if (_outputView is null) return;
            _outputView.Text = string.Join("\n", logLines);

            // Auto-Scroll
            scrollView?.Post(() =>
                scrollView.FullScroll(FocusSearchDirection.Down));
        }
        else
        {
            logLineBuilder.Append(text);
            logLines.Last!.Value = text.ToString();

            if (_outputView is null) return;
            _outputView.Text = string.Join("\n", logLines);
        }        
    }

    private void AppendLine(string line)
    {
        if (logLineBuilder.Length > 0)
        {
            line = logLineBuilder.ToString() + line;
            logLineBuilder.Clear();

            logLines.Last!.Value = line;

            if (_outputView is null) return;
            _outputView.Text = string.Join("\n", logLines);
        }
        else
        {
            logLines.AddLast(line);
            while (logLines.Count > MaxLines)
                logLines.RemoveFirst();

            if (_outputView is null) return;
            _outputView.Text = string.Join("\n", logLines);

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
}
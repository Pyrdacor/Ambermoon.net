using System.Text;
using Android.Util;

namespace AmbermoonAndroid;

public class AndroidConsole(string tag) : TextWriter
{
    private readonly StringBuilder lineBuilder = new();

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string value)
    {
        value ??= "";

        if (lineBuilder.Length > 0)
        {
            value = lineBuilder.ToString() + value;
            lineBuilder.Clear();
        }

        Log.Debug(tag, value);
    }

    public override void Write(string value)
    {
        lineBuilder.Append(value);
    }
}

using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy
{
    internal class Text : IText
    {
        // Key = glyphs, Value = visible length (so without color or newline glyphs)
        readonly List<KeyValuePair<byte[], int>> lines = new List<KeyValuePair<byte[], int>>();

        public Text(List<KeyValuePair<byte[], int>> glyphLines)
            : this(glyphLines.SelectMany(line => line.Key).ToArray(), glyphLines.Count, glyphLines.Count == 0 ? 0 : glyphLines.Max(line => line.Value))
        {
            lines = glyphLines;
        }

        Text(byte[] glyphIndices, int lineCount, int maxLineSize)
        {
            GlyphIndices = glyphIndices;
            LineCount = lineCount;
            MaxLineSize = maxLineSize;
        }

        internal IEnumerable<KeyValuePair<byte[], int>> InternalLines => lines;
        public IReadOnlyList<byte[]> Lines => lines.Select(line => line.Key).ToList().AsReadOnly();
        public byte[] GlyphIndices { get; }
        public int LineCount { get; }
        public int MaxLineSize { get; }
    }

    public class TextProcessor : ITextProcessor
    {
        static byte CharToGlyph(char ch, bool rune, char? fallbackChar = null)
        {
            if (ch >= 'a' && ch <= 'z')
                return (byte)(ch - 'a' + (rune ? 64 : 0));
            else if (ch >= 'A' && ch <= 'Z')
                return (byte)(ch - 'A' + (rune ? 64 : 0));
            else if (ch == 'ä' || ch == 'Ä')
                return (byte)(rune ? 90 : 26);
            else if (ch == 'ü' || ch == 'Ü')
                return (byte)(rune ? 91 : 27);
            else if (ch == 'ö' || ch == 'Ö')
                return (byte)(rune ? 92 : 28);
            else if (ch == 'ß')
                return (byte)(rune ? 93 : 29);
            else if (ch == ';')
                return 30;
            else if (ch == ':')
                return 31;
            else if (ch == ',')
                return 32;
            else if (ch == '.')
                return 33;
            else if (ch == '\'' || ch == '´' || ch == '`')
                return 34;
            else if (ch == '"')
                return 35;
            else if (ch == '!')
                return 36;
            else if (ch == '?')
                return 37;
            else if (ch == '*')
                return 38;
            else if (ch == '_')
                return 39;
            else if (ch == '(')
                return 40;
            else if (ch == ')')
                return 41;
            else if (ch == '%')
                return 42;
            else if (ch == '/')
                return 43;
            else if (ch == '#')
                return 44;
            else if (ch == '-')
                return 45;
            else if (ch == '+')
                return 46;
            else if (ch == '=')
                return 47;
            else if (ch >= '0' && ch <= '9')
                return (byte)(ch - '0' + 48);
            else if (ch == '&')
                return 58;
            else if (ch == 'á' || ch == 'à' || ch == 'â' || ch == 'Á' || ch == 'À' || ch == 'Â')
                return 59;
            else if (ch == 'é' || ch == 'è' || ch == 'ê' || ch == 'É' || ch == 'È' || ch == 'Ê')
                return 60;
            else if (ch == '¢')
                return 61;
            else if (ch == 'û' || ch == 'Û')
                return 62;
            else if (ch == 'ô' || ch == 'Ô')
                return 63;
            else if (ch == ' ')
                return (byte)SpecialGlyph.SoftSpace;
            else if (ch == '$')
                return (byte)SpecialGlyph.HardSpace;
            else if (ch == '^')
                return (byte)SpecialGlyph.NewLine;
            else if (fallbackChar != null)
                return CharToGlyph(fallbackChar.Value, rune);
            else
                throw new AmbermoonException(ExceptionScope.Data, $"Unsupported text character '{ch}'.");
        }

        public bool IsValidCharacter(char ch)
        {
            try
            {
                CharToGlyph(ch, false);
                return true;
            }
            catch (AmbermoonException)
            {
                return false;
            }
        }

        public IText CreateText(string text, char? fallbackChar = null)
        {
            return FinalizeText(text.Select(ch => CharToGlyph(ch, false, fallbackChar)));
        }

        public IText FinalizeText(IEnumerable<byte> glyphs)
        {
            List<KeyValuePair<byte[], int>> glyphLines = new List<KeyValuePair<byte[], int>>();
            int currentLineSize = 0;
            int numLines = 0;
            List<byte> line = new List<byte>();

            void NewLine()
            {
                glyphLines.Add(new KeyValuePair<byte[], int>(line.ToArray(), currentLineSize));
                currentLineSize = 0;
                line.Clear();
                ++numLines;
            }

            foreach (var glyph in glyphs)
            {
                line.Add(glyph);

                if (glyph == (byte)SpecialGlyph.NewLine)
                    NewLine();
                else if (glyph < (byte)SpecialGlyph.FirstColor)
                    ++currentLineSize;
            }

            if (line.LastOrDefault() != (byte)SpecialGlyph.NewLine)
                NewLine();

            return new Text(glyphLines);
        }

        public IText GetLines(IText text, int lineOffset, int numLines)
        {
            var lines = (text as Text).InternalLines.Skip(lineOffset);
            lines = lines.Take(Util.Min(numLines, lines.Count()));
            return new Text(lines.ToList());
        }

        public IText WrapText(IText text, Rect bounds, Size glyphSize)
        {
            int x = bounds.Left;
            int y = bounds.Top;
            int lastSpaceIndex = -1;
            int currentLineSize = 0;
            int maxLineWidth = 0;
            int height = 0;
            var wrappedGlyphLines = new List<KeyValuePair<byte[], int>>();
            var line = new List<byte>();

            void NewLine(int newX = 0)
            {
                if (x > maxLineWidth)
                    maxLineWidth = x;

                lastSpaceIndex = -1;
                x = bounds.Left + newX;
                y += glyphSize.Height;
                height = y;
                wrappedGlyphLines.Add(new KeyValuePair<byte[], int>(line.ToArray(), currentLineSize));
                line.Clear();
                currentLineSize = 0;
            }

            byte? LastGlyph() => line.Count == 0 ? wrappedGlyphLines.Count == 0 ? (byte?)null :
                wrappedGlyphLines.Last().Key.LastOrDefault() : line.LastOrDefault();

            foreach (var glyph in text.GlyphIndices)
            {
                switch (glyph)
                {
                    case (byte)SpecialGlyph.SoftSpace:
                        if (LastGlyph() == (byte)SpecialGlyph.NewLine)
                            continue;
                        x += glyphSize.Width;
                        if (x > bounds.Right)
                        {
                            line.Add((byte)SpecialGlyph.NewLine);
                            NewLine();
                        }
                        else
                        {
                            lastSpaceIndex = line.Count;
                            line.Add(glyph);
                            ++currentLineSize;
                        }
                        break;
                    case (byte)SpecialGlyph.NewLine:
                        line.Add(glyph);
                        NewLine();
                        break;
                    case (byte)SpecialGlyph.FirstColor:
                        line.Add(glyph);
                        break;
                    default:
                    {
                        line.Add(glyph);
                        ++currentLineSize;
                        x += glyphSize.Width;
                        if (x > bounds.Right)
                        {
                            if (lastSpaceIndex == -1)
                                throw new AmbermoonException(ExceptionScope.Data, "Text can not be wrapped inside the given bounds.");

                            line[lastSpaceIndex] = (byte)SpecialGlyph.NewLine;
                            int nextLineSize = (currentLineSize - lastSpaceIndex - 1) * glyphSize.Width;
                            var tempLine = line.Skip(lastSpaceIndex + 1);
                            line = line.Take(lastSpaceIndex + 1).ToList();
                            currentLineSize = lastSpaceIndex;
                            x = currentLineSize * glyphSize.Width;
                            NewLine(nextLineSize);
                            line = tempLine.ToList();
                            currentLineSize = line.Count;
                        }
                        break;
                    }
                }
            }

            if (LastGlyph() == (byte)SpecialGlyph.NewLine)
                wrappedGlyphLines[^1].Key[^1] = (byte)SpecialGlyph.SoftSpace;

            if (line.Count > 0)
                wrappedGlyphLines.Add(new KeyValuePair<byte[], int>(line.ToArray(), currentLineSize));

            // Note: The added 1 is used as after the last new line character there are always other characters.
            return new Text(wrappedGlyphLines);
        }

        public IText ProcessText(string text, ITextNameProvider nameProvider, List<string> dictionary, char? fallbackChar = null)
        {
            List<byte> glyphIndices = new List<byte>();

            text = text.Replace("~LEAD~", nameProvider.LeadName);
            text = text.Replace("~SELF~", nameProvider.SelfName);
            text = text.Replace("~CAST~", nameProvider.CastName);
            text = text.Replace("~INVN~", nameProvider.InvnName);
            text = text.Replace("~SUBJ~", nameProvider.SubjName);
            text = text.Replace("~SEX1~", nameProvider.Sex1Name);
            text = text.Replace("~SEX2~", nameProvider.Sex2Name);

            bool rune = false;
            int tagStart = -1;
            int dictRefStart = -1;

            void ProcessTag(string name)
            {
                if (name == "RUN1")
                    rune = true;
                else if (name == "NORM")
                    rune = false;
                else if (name.StartsWith("INK"))
                {
                    if (!int.TryParse(name.Substring(3), out int colorIndex) || colorIndex < 0 || colorIndex > 31)
                        throw new AmbermoonException(ExceptionScope.Data, $"Invalid ink tag: ~{name}~");

                    glyphIndices.Add((byte)(SpecialGlyph.FirstColor + colorIndex));
                }
                else
                    throw new AmbermoonException(ExceptionScope.Data, $"Unknown tag: ~{name}~");
            }

            for (int i = 0; i < text.Length; ++i)
            {
                if (text[i] == '~')
                {
                    if (dictRefStart != -1)
                        throw new AmbermoonException(ExceptionScope.Data, "Tag inside a dictionary reference.");

                    if (tagStart == -1)
                        tagStart = i + 1;
                    else
                    {
                        ProcessTag(text[tagStart..i]);
                        tagStart = -1;
                    }
                }
                else if (tagStart != -1)
                {
                    continue;
                }
                else if (text[i] == '>')
                {
                    if (dictRefStart != -1)
                        throw new AmbermoonException(ExceptionScope.Data, "A second dictionary reference started before the last was closed.");

                    dictRefStart = i + 1;
                }
                else if (text[i] == '<')
                {
                    if (dictRefStart == -1)
                        throw new AmbermoonException(ExceptionScope.Data, "Closing dictionary reference without starting one.");

                    dictionary.Add(text[dictRefStart..i].Trim());
                    dictRefStart = -1;
                }
                else
                {
                    glyphIndices.Add(CharToGlyph(text[i], rune, fallbackChar));
                }
            }

            if (tagStart != -1)
                throw new AmbermoonException(ExceptionScope.Data, $"Not closed tag at position {tagStart - 1}.");
            if (dictRefStart != -1)
                throw new AmbermoonException(ExceptionScope.Data, $"Not closed dictionary reference at position {dictRefStart - 1}.");

            return FinalizeText(glyphIndices);
        }
    }
}

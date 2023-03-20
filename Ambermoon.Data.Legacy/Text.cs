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
        public Size WrappedSize { get; set; }
        public Size WrappedGlyphSize { get; set; }
    }

    public class TextProcessor : ITextProcessor
    {
        readonly int glyphCount;

        public TextProcessor(int glyphCount)
        {
            // The original version has 94 glyphs.
            // CHaracters like á, à and â share the same glyph
            // as it wasn't possible to distinguish them in such
            // small resolution. But with modern fonts there might
            // by distinct glyphs for all of them. So when this
            // count is > 94, we have more character glyphs available.
            this.glyphCount = glyphCount;
        }

        byte CharToGlyph(char ch, bool rune, char? fallbackChar = null)
        {
            bool extended = glyphCount > 94;

            if (ch >= 'a' && ch <= 'z')
                return (byte)(ch - 'a' + (rune ? 64 : 0));
            else if (ch >= 'A' && ch <= 'Z')
                return (byte)(ch - 'A' + (rune ? 64 : 0));
            else if (ch == 'ä' || ch == 'Ä')
                return (byte)(rune ? 92 : 26);
            else if (ch == 'ü' || ch == 'Ü')
                return (byte)(rune ? 91 : 27);
            else if (ch == 'ö' || ch == 'Ö')
                return (byte)(rune ? 90 : 28);
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
            else if (ch == 'á' || ch == 'Á')
                return (byte)(rune ? 64 : extended ? 94 : 59);
            else if (ch == 'à' || ch == 'À')
                return (byte)(rune ? 64 : 59);
            else if (ch == 'â' || ch == 'Â')
                return (byte)(rune ? 64 : extended ? 95 : 59);
            else if (ch == 'ê' || ch == 'Ê')
                return (byte)(rune ? 68 : 60);
            else if (ch == 'è' || ch == 'È')
                return (byte)(rune ? 68 : extended ? 96 : 60);
            else if (ch == 'é' || ch == 'É')
                return (byte)(rune ? 68 : extended ? 97 : 60);
            else if (ch == 'ç' || ch == '\u0063')
                return (byte)(rune ? 66 : 61);
            else if (ch == '¢')
                return (byte)(rune ? 66 : extended ? 98 : 61);
            else if (ch == 'û' || ch == 'Û')
                return (byte)(rune ? 84 : 62);
            else if (ch == 'ô' || ch == 'Ô')
                return (byte)(rune ? 78 : 63);
            else if (ch == 'î' || ch == 'Î')
                return (byte)(rune ? 72 : extended ? 99 : 8);
            else if (ch == 'ë' || ch == 'Ë')
                return (byte)(rune ? 68 : extended ? 100 : 60);
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
                while (line.Count != 0 && line.Last() == (byte)SpecialGlyph.SoftSpace)
                {
                    // Trim end
                    line.RemoveAt(line.Count - 1);
                    --currentLineSize;
                }

                glyphLines.Add(new KeyValuePair<byte[], int>(line.ToArray(), currentLineSize));
                currentLineSize = 0;
                line.Clear();
                ++numLines;
            }

            foreach (var glyph in glyphs)
            {
                if (currentLineSize == 0 && line.Count != 0 && glyph == (byte)SpecialGlyph.SoftSpace)
                    continue; // Trim start

                line.Add(glyph);

                if (glyph == (byte)SpecialGlyph.NewLine)
                    NewLine();
                else if (glyph < (byte)SpecialGlyph.NoTrim)
                    ++currentLineSize;
            }

            if (line.LastOrDefault() != (byte)SpecialGlyph.NewLine)
                NewLine();

            return new Text(glyphLines);
        }

        public IText GetLines(IText text, int lineOffset, int numLines)
        {
            if (lineOffset >= 0)
            {
                var lines = (text as Text).InternalLines.Skip(lineOffset);
                lines = lines.Take(Util.Min(numLines, lines.Count()));
                return new Text(lines.ToList());
            }
            else if (lineOffset == -numLines)
            {
                return new Text(Enumerable.Repeat(KeyValuePair.Create(new byte[1] { (byte)SpecialGlyph.NewLine }, 1), numLines).ToList());
            }
            else
            {
                var emptyLines = Enumerable.Repeat(KeyValuePair.Create(new byte[1] { (byte)SpecialGlyph.NewLine }, -lineOffset), -lineOffset).ToList();
                int remainingLines = numLines + lineOffset;
                var lines = (text as Text).InternalLines;
                emptyLines.AddRange(lines.Take(Util.Min(remainingLines, lines.Count())));
                return new Text(emptyLines);
            }
        }

        public IText WrapText(IText text, Rect bounds, Size glyphSize)
        {
            if (text.WrappedSize?.Width == bounds.Width && text.WrappedSize?.Height >= bounds.Height &&
                text.WrappedGlyphSize == glyphSize)
                return text;

            int x = bounds.Left;
            int y = bounds.Top;
            int lastSpaceIndex = -1;
            int currentLineSize = 0;
            int maxLineWidth = 0;
            int height = 0;
            var wrappedGlyphLines = new List<KeyValuePair<byte[], int>>();
            var line = new List<byte>();

            void NewLine()
            {
                if (x > bounds.Left + maxLineWidth)
                    maxLineWidth = x - bounds.Left;

                lastSpaceIndex = -1;
                x = bounds.Left;
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
                            x -= glyphSize.Width;
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
                    default:
                    {
                        if (glyph >= (byte)SpecialGlyph.NoTrim)
                        {
                            line.Add(glyph);
                        }
                        else
                        {
                            x += glyphSize.Width;
                            if (x > bounds.Right)
                            {
                                if (lastSpaceIndex != -1)
                                {
                                    line.Add(glyph);
                                    ++currentLineSize;
                                    line[lastSpaceIndex] = (byte)SpecialGlyph.NewLine;
                                    var newLine = line.Skip(lastSpaceIndex + 1);
                                    line = line.Take(lastSpaceIndex + 1).ToList();
                                    currentLineSize = line.Count(c => c < (byte)SpecialGlyph.NoTrim);
                                    x = bounds.Left + (currentLineSize - 1) * glyphSize.Width;
                                    NewLine();
                                    currentLineSize = newLine.Count(c => c < (byte)SpecialGlyph.NoTrim);
                                    x = bounds.Left + currentLineSize * glyphSize.Width;
                                    line = newLine.ToList();
                                }
                                else
                                {
                                    line.Add((byte)SpecialGlyph.NewLine);
                                    NewLine();
                                    line.Add(glyph);
                                    currentLineSize = 1;
                                }
                            }
                            else
                            {
                                line.Add(glyph);
                                ++currentLineSize;
                            }
                        }
                        break;
                    }
                }
            }

            if (LastGlyph() == (byte)SpecialGlyph.NewLine)
                wrappedGlyphLines[^1].Key[^1] = (byte)SpecialGlyph.SoftSpace;

            if (line.Count > 0)
                wrappedGlyphLines.Add(new KeyValuePair<byte[], int>(line.ToArray(), currentLineSize));

            return new Text(wrappedGlyphLines)
            {
                WrappedSize = new Size(bounds.Size),
                WrappedGlyphSize = new Size(glyphSize)
            };
        }

        public IText ProcessText(string text, ITextNameProvider nameProvider, List<string> dictionary, char? fallbackChar = null)
        {
            var glyphIndices = new List<byte>();

            if (nameProvider != null)
            {
                text = text.Replace("~LEAD~", nameProvider.LeadName);
                text = text.Replace("~SELF~", nameProvider.SelfName);
                text = text.Replace("~CAST~", nameProvider.CastName);
                text = text.Replace("~INVN~", nameProvider.InvnName);
                text = text.Replace("~SUBJ~", nameProvider.SubjName);
                text = text.Replace("~SEX1~", nameProvider.Sex1Name);
                text = text.Replace("~SEX2~", nameProvider.Sex2Name);
            }

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
                    if (!int.TryParse(name.Substring(3), out int colorIndex) || colorIndex < 0 || colorIndex > 32)
                        throw new AmbermoonException(ExceptionScope.Data, $"Invalid ink tag: ~{name}~");

                    glyphIndices.Add((byte)(SpecialGlyph.FirstColor + colorIndex));
                }
                else
                {
                    throw new AmbermoonException(ExceptionScope.Data, $"Unknown tag: ~{name}~");
                }
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
                        if (tagStart > 1 && text[tagStart - 2] == ' ')
                            glyphIndices.Insert(glyphIndices.Count - 1, (byte)SpecialGlyph.NoTrim);

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
                    if (glyphIndices.Count != 0 && glyphIndices.Last() >= (byte)SpecialGlyph.FirstColor)
                        glyphIndices.Add((byte)SpecialGlyph.NoTrim);

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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System;

namespace Ambermoon.Data.Legacy
{
    internal class Text : IText
    {
        // Key = glyphs, Value = visible length (so without color or newline glyphs)
        readonly List<KeyValuePair<byte[], int>> lines = [];

        public Text(List<KeyValuePair<byte[], int>> glyphLines)
            : this([.. glyphLines.SelectMany(line => line.Key)], glyphLines.Count, glyphLines.Count == 0 ? 0 : glyphLines.Max(line => line.Value))
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
            // Characters like á, à and â share the same glyph
            // as it wasn't possible to distinguish them in such
            // small resolution. But with modern fonts there might
            // by distinct glyphs for all of them. So when this
            // count is > 94, we have more character glyphs available.
            this.glyphCount = glyphCount;
        }

        public static string RemoveDiacritics(string text)
        {
	        string normalizedText = text.Normalize(NormalizationForm.FormD);
	        StringBuilder stringBuilder = new StringBuilder();

	        foreach (char c in normalizedText)
	        {
		        if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
			        stringBuilder.Append(c);
	        }

	        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

		IEnumerable<byte> CharToGlyph(char ch, bool rune, char? fallbackChar = null)
        {
            bool extended = glyphCount > 94;

            if (ch >= 'a' && ch <= 'z')
                yield return (byte)(ch - 'a' + (rune ? 64 : 0));
            else if (ch >= 'A' && ch <= 'Z')
                yield return (byte)(ch - 'A' + (rune ? 64 : 0));
            else if (ch == 'ä' || ch == 'Ä')
                yield return (byte)(rune ? 92 : 26);
            else if (ch == 'ü' || ch == 'Ü')
                yield return (byte)(rune ? 91 : 27);
            else if (ch == 'ö' || ch == 'Ö')
                yield return (byte)(rune ? 90 : 28);
            else if (ch == 'ß')
                yield return (byte)(rune ? 93 : 29);
            else if (ch == ';')
                yield return 30;
            else if (ch == ':')
                yield return 31;
            else if (ch == ',')
                yield return 32;
            else if (ch == '.')
                yield return 33;
            else if (ch == '\'' || ch == '´' || ch == '`')
                yield return 34;
            else if (ch == '"')
                yield return 35;
            else if (ch == '!')
                yield return 36;
            else if (ch == '?')
                yield return 37;
            else if (ch == '*')
                yield return 38;
            else if (ch == '_')
                yield return 39;
            else if (ch == '(')
                yield return 40;
            else if (ch == ')')
                yield return 41;
            else if (ch == '%')
                yield return 42;
            else if (ch == '/')
                yield return 43;
            else if (ch == '#')
                yield return 44;
            else if (ch == '-')
                yield return 45;
            else if (ch == '+')
                yield return 46;
            else if (ch == '=')
                yield return 47;
            else if (ch >= '0' && ch <= '9')
                yield return (byte)(ch - '0' + 48);
            else if (ch == '&')
                yield return 58;
            else if (ch == 'á' || ch == 'Á')
                yield return (byte)(rune ? 64 : extended ? 94 : 59);
            else if (ch == 'à' || ch == 'À')
                yield return (byte)(rune ? 64 : 59);
            else if (ch == 'â' || ch == 'Â')
                yield return (byte)(rune ? 64 : extended ? 95 : 59);
            else if (ch == 'ê' || ch == 'Ê')
                yield return (byte)(rune ? 68 : 60);
            else if (ch == 'è' || ch == 'È')
                yield return (byte)(rune ? 68 : extended ? 96 : 60);
            else if (ch == 'é' || ch == 'É')
                yield return (byte)(rune ? 68 : extended ? 97 : 60);
            else if (ch == 'ç' || ch == '\u0063')
                yield return (byte)(rune ? 66 : 61);
            else if (ch == '¢')
                yield return (byte)(rune ? 66 : extended ? 98 : 61);
            else if (ch == 'û' || ch == 'Û')
                yield return (byte)(rune ? 84 : 62);
            else if (ch == 'ô' || ch == 'Ô')
                yield return (byte)(rune ? 78 : 63);
            else if (ch == 'æ' || ch == 'Æ')
            {
                if (extended)
                    yield return 101;
                else
                {
                    yield return (byte)(rune ? 64 : 0); // A
                    yield return (byte)(rune ? 68 : 4); // E
                }
            }
            else if (ch == 'œ' || ch == 'ɶ' || ch == 'Œ')
            {
                if (extended)
                    yield return 102;
                else
                {
                    yield return (byte)(rune ? 78 : 14); // O
                    yield return (byte)(rune ? 68 : 4); // E
                }
            }
            else if (ch == 'î' || ch == 'Î')
                yield return (byte)(rune ? 72 : extended ? 99 : 8);
            else if (ch == 'ë' || ch == 'Ë')
                yield return (byte)(rune ? 68 : extended ? 100 : 60);
            else if (ch == 'ą' || ch == 'Ą')
                yield return (byte)(rune ? 64 : extended ? 103 : 0);
            else if (ch == 'ć' || ch == 'Ć')
                yield return (byte)(rune ? 66 : extended ? 104 : 2);
            else if (ch == 'ę' || ch == 'Ę')
                yield return (byte)(rune ? 68 : extended ? 105 : 4);
            else if (ch == 'ł' || ch == 'Ł')
                yield return (byte)(rune ? 75 : extended ? 106 : 11);
            else if (ch == 'ń' || ch == 'Ń')
                yield return (byte)(rune ? 77 : extended ? 107 : 13);
            else if (ch == 'ó' || ch == 'Ó')
                yield return (byte)(rune ? 78 : extended ? 108 : 63);
            else if (ch == 'ś' || ch == 'Ś')
                yield return (byte)(rune ? 82 : extended ? 109 : 18);
            else if (ch == 'ź' || ch == 'Ź')
                yield return (byte)(rune ? 89 : extended ? 110 : 25);
            else if (ch == 'ż' || ch == 'Ż')
                yield return (byte)(rune ? 89 : extended ? 111 : 25);
            else if (ch == 'í' || ch == 'Í')
	            yield return (byte)(rune ? 72 : extended ? 112 : 8);
            else if (ch == 'ů' || ch == 'Ů')
	            yield return (byte)(rune ? 84 : extended ? 113 : 62);
            else if (ch == 'č' || ch == 'Č')
	            yield return (byte)(rune ? 66 : extended ? 114 : 2);
            else if (ch == 'ď' || ch == 'Ď')
	            yield return (byte)(rune ? 67 : extended ? 115 : 3);
            else if (ch == 'ě' || ch == 'Ě')
	            yield return (byte)(rune ? 68 : extended ? 116 : 4);
            else if (ch == 'ň' || ch == 'Ň')
	            yield return (byte)(rune ? 77 : extended ? 117 : 13);
            else if (ch == 'ř' || ch == 'Ř')
	            yield return (byte)(rune ? 81 : extended ? 118 : 17);
            else if (ch == 'š' || ch == 'Š')
	            yield return (byte)(rune ? 82 : extended ? 119 : 18);
            else if (ch == 'ť' || ch == 'Ť')
	            yield return (byte)(rune ? 83 : extended ? 120 : 19);
            else if (ch == 'ý' || ch == 'Ý')
	            yield return (byte)(rune ? 88 : extended ? 121 : 24);
            else if (ch == 'ž' || ch == 'Ž')
	            yield return (byte)(rune ? 89 : extended ? 122 : 25);
            else if (ch == 'ú' || ch == 'Ú')
	            yield return (byte)(rune ? 84 : extended ? 123 : 62);
			else if (ch == ' ')
                yield return (byte)SpecialGlyph.SoftSpace;
            else if (ch == '$')
                yield return (byte)SpecialGlyph.HardSpace;
            else if (ch == '^')
                yield return (byte)SpecialGlyph.NewLine;
            else if (fallbackChar != null)
            {
                foreach (var glyph in CharToGlyph(fallbackChar.Value, rune))
                    yield return glyph;
            }
            else if (RemoveDiacritics(ch.ToString()).Any(c => c > 32 && c < 128))
            {
                var glyph = CharToGlyph(RemoveDiacritics(ch.ToString()).First(c => c > 32 && c < 128), rune, ' ').First();

                if (glyph == (byte)SpecialGlyph.SoftSpace)
                    throw new AmbermoonException(ExceptionScope.Data, $"Unsupported text character '{ch}'.");

                yield return glyph;
            }
            else
                throw new AmbermoonException(ExceptionScope.Data, $"Unsupported text character '{ch}'.");
        }

        public bool IsValidCharacter(char ch)
        {
            try
            {
                CharToGlyph(ch, false).Any(); // Any is needed to evaluate the IEnumerable immediately
                return true;
            }
            catch (AmbermoonException)
            {
                return false;
            }
        }

        public IText CreateText(string text, char? fallbackChar = null)
        {
            return FinalizeText(text.SelectMany(ch => CharToGlyph(ch, false, fallbackChar)));
        }

        public static IText FinalizeText(IEnumerable<byte> glyphs)
        {
            List<KeyValuePair<byte[], int>> glyphLines = [];
            int currentLineSize = 0;
            int numLines = 0;
            List<byte> line = [];

            void NewLine()
            {
                int index = line.Count - 1;

                while (line.Count != 0 && index >= 0)
                {
                    byte last = line[index];

                    // Trim end
                    if (last == (byte)SpecialGlyph.SoftSpace)
                    {
                        line.RemoveAt(index);
                        --currentLineSize;
                        --index;
                    }
                    // Remove trailing no trim markers
                    else if (last == (byte)SpecialGlyph.NoTrim)
                    {
                        line.RemoveAt(index);
                        --index;
                    }
                    else if (last >= (byte)SpecialGlyph.FirstColor)
                    {
                        --index;
                    }
                    else
                    {
                        break;
                    }
                }

                glyphLines.Add(new KeyValuePair<byte[], int>([.. line], currentLineSize));
                currentLineSize = 0;
                line.Clear();
                ++numLines;
            }

            foreach (var glyph in glyphs)
            {
                if (currentLineSize == 0 && line.Count != 0 && glyph == (byte)SpecialGlyph.SoftSpace && line.Last() != (byte)SpecialGlyph.NoTrim)
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
            bool addedSoftspaceNewline = false;

            void NewLine()
            {
                // Trim end
                int index = line.Count - 2;

                while (index >= 0)
                {
                    byte last = line[index];

                    if (last == (byte)SpecialGlyph.SoftSpace)
                    {
                        line.RemoveAt(index);
                        --currentLineSize;
                        --index;
                        x -= glyphSize.Width;
                    }
                    else if (last == (byte)SpecialGlyph.NoTrim)
                    {
                        line.RemoveAt(index);
                        --index;
                    }
                    else if (last >= (byte)SpecialGlyph.FirstColor)
                    {
                        --index;
                    }
                    else
                    {
                        break;
                    }
                }

                if (x > bounds.Left + maxLineWidth)
                    maxLineWidth = x - bounds.Left;

                lastSpaceIndex = -1;
                x = bounds.Left;
                y += glyphSize.Height;
                height = y;
                wrappedGlyphLines.Add(new KeyValuePair<byte[], int>([.. line], currentLineSize));
                line.Clear();
                currentLineSize = 0;
            }

            byte? LastGlyph() => line.Count != 0 ? line.LastOrDefault() :
                (wrappedGlyphLines.Count == 0 ? null : wrappedGlyphLines.Last().Key.LastOrDefault());

            int index = -1; // 362

            foreach (var glyph in text.GlyphIndices)
            {
                ++index;

                if (glyph != (byte)SpecialGlyph.NewLine)
                    addedSoftspaceNewline = false;

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
                            addedSoftspaceNewline = true;
                        }
                        else
                        {
                            lastSpaceIndex = line.Count;
                            line.Add(glyph);
                            ++currentLineSize;
                        }
                        break;
                    case (byte)SpecialGlyph.NewLine:
                        if (addedSoftspaceNewline)
                        {
                            addedSoftspaceNewline = false;
                            continue;
                        }
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
                                    line = [.. line.Take(lastSpaceIndex + 1)];
                                    currentLineSize = line.Count(c => c < (byte)SpecialGlyph.NewLine);
                                    x = bounds.Left + currentLineSize * glyphSize.Width;
                                    NewLine();
                                    currentLineSize = newLine.Count(c => c < (byte)SpecialGlyph.NewLine);
                                    x = bounds.Left + currentLineSize * glyphSize.Width;
                                    line = [.. newLine];
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
                wrappedGlyphLines.Add(new KeyValuePair<byte[], int>([.. line], currentLineSize));

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
                else if (name.StartsWith("INK "))
                {
                    if (!int.TryParse(name.AsSpan(4), out int colorIndex) || colorIndex < 0 || colorIndex > 32)
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

                    glyphIndices.AddRange(CharToGlyph(text[i], rune, fallbackChar));
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

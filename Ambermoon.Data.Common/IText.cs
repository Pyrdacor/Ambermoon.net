using System.Collections.Generic;

namespace Ambermoon.Data
{
    public enum SpecialGlyph
    {
        SoftSpace = 94, // expressed with a normal space character
        HardSpace, // expressed with $
        NewLine, // expressed with ^
        FirstColor // everything >= this is a color from 0 to 31
    }

    public interface ITextNameProvider
    {
        /// <summary>
        /// Main character name
        /// </summary>
        string LeadName { get; }
        /// <summary>
        /// Active character name
        /// </summary>
        string SelfName { get; }
        /// <summary>
        /// Current caster name
        /// </summary>
        string CastName { get; }
        /// <summary>
        /// Current inventory owner name
        /// </summary>
        string InvnName { get; }
        /// <summary>
        /// Current subject name
        /// </summary>
        string SubjName { get; }
        /// <summary>
        /// Current sex-dependent 3rd person pronoun (e.g. 'he' or 'she')
        /// </summary>
        string Sex1Name { get; }
        /// <summary>
        /// Current sex-dependent 3rd person possessive determiner (e.g. 'his' or 'her')
        /// </summary>
        string Sex2Name { get; }
    }

    public interface ITextProcessor
    {
        IText ProcessText(string text, ITextNameProvider nameProvider, List<string> dictionary);
        IText CreateText(string text);
        /// <summary>
        /// Wraps a given text so it fits into the given bounds.
        /// 
        /// Note that the height still can exceed the bound height.
        /// In this case the text must be scrolled to view all of it.
        /// This only wraps text lines to keep them inside the bound width.
        /// </summary>
        IText WrapText(IText text, Rect bounds, Size glyphSize);
        IText GetLines(IText text, int lineOffset, int numLines);
    }

    public interface IText
    {
        public IReadOnlyList<byte[]> Lines { get; }
        public byte[] GlyphIndices { get; }
        public int LineCount { get; }
        public int MaxLineSize { get; }
    }
}

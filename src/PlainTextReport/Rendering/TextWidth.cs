//
// The MIT License (MIT)
//
// Copyright (c) 2024 Federico Mazzanti
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mafesoft.PlainTextReport
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Measures the text in columns of a monospaced screen or printer, per text element (grapheme):
    /// East Asian wide and fullwidth characters and most emoji take 2 columns, the combining marks and the
    /// zero-width characters take 0, the others 1. A text element is never cut in half.
    /// The simple mode counts the UTF-16 code units, as <see cref="string.Length"/>.
    /// </summary>
    internal sealed class TextWidth
    {
        /// <summary>
        /// Measures the display width of the text elements
        /// </summary>
        public static readonly TextWidth Unicode = new TextWidth(true);

        /// <summary>
        /// Counts the UTF-16 code units, as <see cref="string.Length"/>: faster, for texts without wide characters
        /// </summary>
        public static readonly TextWidth Simple = new TextWidth(false);

        // the East Asian Wide (W) and Fullwidth (F) ranges and the emoji with default emoji presentation, sorted
        private static readonly int[] WideRanges =
        {
            0x1100, 0x115F, 0x231A, 0x231B, 0x2329, 0x232A, 0x23E9, 0x23EC, 0x23F0, 0x23F0, 0x23F3, 0x23F3,
            0x25FD, 0x25FE, 0x2614, 0x2615, 0x2648, 0x2653, 0x267F, 0x267F, 0x2693, 0x2693, 0x26A1, 0x26A1,
            0x26AA, 0x26AB, 0x26BD, 0x26BE, 0x26C4, 0x26C5, 0x26CE, 0x26CE, 0x26D4, 0x26D4, 0x26EA, 0x26EA,
            0x26F2, 0x26F3, 0x26F5, 0x26F5, 0x26FA, 0x26FA, 0x26FD, 0x26FD, 0x2705, 0x2705, 0x270A, 0x270B,
            0x2728, 0x2728, 0x274C, 0x274C, 0x274E, 0x274E, 0x2753, 0x2755, 0x2757, 0x2757, 0x2795, 0x2797,
            0x27B0, 0x27B0, 0x27BF, 0x27BF, 0x2B1B, 0x2B1C, 0x2B50, 0x2B50, 0x2B55, 0x2B55,
            0x2E80, 0x303E, 0x3041, 0x33FF, 0x3400, 0x4DBF, 0x4E00, 0x9FFF, 0xA000, 0xA4CF, 0xA960, 0xA97F,
            0xAC00, 0xD7A3, 0xF900, 0xFAFF, 0xFE10, 0xFE19, 0xFE30, 0xFE6F, 0xFF00, 0xFF60, 0xFFE0, 0xFFE6,
            0x16FE0, 0x16FE4, 0x17000, 0x18CFF, 0x1B000, 0x1B2FF,
            0x1F004, 0x1F004, 0x1F0CF, 0x1F0CF, 0x1F18E, 0x1F18E, 0x1F191, 0x1F19A, 0x1F200, 0x1F2FF,
            0x1F300, 0x1F320, 0x1F32D, 0x1F335, 0x1F337, 0x1F37C, 0x1F37E, 0x1F393, 0x1F3A0, 0x1F3CA,
            0x1F3CF, 0x1F3D3, 0x1F3E0, 0x1F3F0, 0x1F3F4, 0x1F3F4, 0x1F3F8, 0x1F43E, 0x1F440, 0x1F440,
            0x1F442, 0x1F4FC, 0x1F4FF, 0x1F53D, 0x1F54B, 0x1F54E, 0x1F550, 0x1F567, 0x1F57A, 0x1F57A,
            0x1F595, 0x1F596, 0x1F5A4, 0x1F5A4, 0x1F5FB, 0x1F64F, 0x1F680, 0x1F6C5, 0x1F6CC, 0x1F6CC,
            0x1F6D0, 0x1F6D2, 0x1F6D5, 0x1F6D7, 0x1F6DC, 0x1F6DF, 0x1F6EB, 0x1F6EC, 0x1F6F4, 0x1F6FC,
            0x1F7E0, 0x1F7EB, 0x1F7F0, 0x1F7F0, 0x1F90C, 0x1F93A, 0x1F93C, 0x1F945, 0x1F947, 0x1F9FF,
            0x1FA70, 0x1FAFF, 0x20000, 0x2FFFD, 0x30000, 0x3FFFD,
        };

        private const char EmojiPresentation = '\uFE0F';

        private readonly bool unicode;

        private TextWidth(bool unicode)
        {
            this.unicode = unicode;
        }

        /// <summary>
        /// Returns the number of columns the text takes
        /// </summary>
        public int GetWidth(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }
            if (!unicode || IsNarrow(text))
            {
                return text.Length;
            }

            var width = 0;
            var starts = StringInfo.ParseCombiningCharacters(text);
            for (int i = 0; i < starts.Length; i++)
            {
                width += GetElementWidth(text, starts[i], i + 1 < starts.Length ? starts[i + 1] : text.Length);
            }
            return width;
        }

        /// <summary>
        /// Returns the length, in UTF-16 code units, of the longest beginning of the text made of whole
        /// text elements that fits in the width
        /// </summary>
        public int GetFitLength(string text, int width)
        {
            if (string.IsNullOrEmpty(text) || width <= 0)
            {
                return 0;
            }
            if (!unicode || IsNarrow(text))
            {
                return Math.Min(text.Length, width);
            }

            var used = 0;
            var starts = StringInfo.ParseCombiningCharacters(text);
            for (int i = 0; i < starts.Length; i++)
            {
                used += GetElementWidth(text, starts[i], i + 1 < starts.Length ? starts[i + 1] : text.Length);
                if (used > width)
                {
                    return starts[i];
                }
            }
            return text.Length;
        }

        /// <summary>
        /// Returns the length, in UTF-16 code units, of the text element that starts at the index
        /// </summary>
        public int GetElementLength(string text, int index)
        {
            if (string.IsNullOrEmpty(text) || index >= text.Length)
            {
                return 0;
            }
            if (!unicode)
            {
                return 1;
            }
            return StringInfo.GetNextTextElement(text, index).Length;
        }

        /// <summary>
        /// True when every character takes exactly one column: the fast path for the Latin texts
        /// </summary>
        private static bool IsNarrow(string text)
        {
            foreach (var c in text)
            {
                if (c >= '\u0300')
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the width of the text element between start and end
        /// </summary>
        private static int GetElementWidth(string text, int start, int end)
        {
            if (text.IndexOf(EmojiPresentation, start, end - start) >= 0)
            {
                // a symbol followed by the emoji variation selector, for example ❤️
                return 2;
            }

            var codePoint = char.IsSurrogatePair(text, start) ? char.ConvertToUtf32(text, start) : text[start];
            switch (CharUnicodeInfo.GetUnicodeCategory(text, start))
            {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.EnclosingMark:
                    return 0;
                case UnicodeCategory.Format:
                    // zero width space, joiners, marks and BOM; the soft hyphen is printed
                    return codePoint == 0x00AD ? 1 : 0;
            }
            return IsWide(codePoint) ? 2 : 1;
        }

        /// <summary>
        /// True when the code point is East Asian wide or fullwidth, or an emoji
        /// </summary>
        private static bool IsWide(int codePoint)
        {
            if (codePoint < WideRanges[0])
            {
                return false;
            }

            // binary search of the range
            int low = 0, high = WideRanges.Length / 2 - 1;
            while (low <= high)
            {
                var middle = (low + high) / 2;
                if (codePoint < WideRanges[middle * 2])
                {
                    high = middle - 1;
                }
                else if (codePoint > WideRanges[middle * 2 + 1])
                {
                    low = middle + 1;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }
    }
}

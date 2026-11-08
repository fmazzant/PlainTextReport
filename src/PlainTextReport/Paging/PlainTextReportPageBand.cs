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
    /// <summary>
    /// A band printed at the top (header) or at the bottom (footer) of every page.
    /// The texts can contain line breaks and these placeholders:
    /// {page}, {pages}, {title}, {date}; a format can be added, for example {page:000} or {date:dd/MM/yyyy HH:mm}.
    /// </summary>
    public sealed class PlainTextReportPageBand
    {
        /// <summary>
        /// Text aligned to the left
        /// </summary>
        public string Left { get; set; }

        /// <summary>
        /// Text centered
        /// </summary>
        public string Center { get; set; }

        /// <summary>
        /// Text aligned to the right
        /// </summary>
        public string Right { get; set; }

        /// <summary>
        /// Draws a line between the band and the body of the page
        /// </summary>
        public bool ShowSeparator { get; set; }

        /// <summary>
        /// Character of the separator line
        /// </summary>
        public char SeparatorChar { get; set; } = '-';

        /// <summary>
        /// Returns true when the band has no text
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(Left) && string.IsNullOrEmpty(Center) && string.IsNullOrEmpty(Right);
    }
}

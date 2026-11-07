/// <summary>
///
/// The MIT License (MIT)
///
/// Copyright (c) 2024 Federico Mazzanti
///
/// Permission is hereby granted, free of charge, to any person
/// obtaining a copy of this software and associated documentation
/// files (the "Software"), to deal in the Software without
/// restriction, including without limitation the rights to use,
/// copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following
/// conditions:
///
/// The above copyright notice and this permission notice shall be
/// included in all copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
/// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
/// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
/// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
/// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
/// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
/// OTHER DEALINGS IN THE SOFTWARE.
///
/// </summary>

namespace Mafesoft.PlainTextReport
{
    /// <summary>
    /// The text alignment inside a column
    /// </summary>
    public enum PlainTextReportAlignment
    {
        /// <summary>
        /// Left aligned
        /// </summary>
        Left,

        /// <summary>
        /// Right aligned
        /// </summary>
        Right,

        /// <summary>
        /// Centered
        /// </summary>
        Center
    }

    /// <summary>
    /// The column style definition
    /// </summary>
    public sealed class PlainTextReportColumnStyle
    {
        /// <summary>
        /// Fixed width of the column, in characters.
        /// When null the width is computed from the header and the values.
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// Alignment of the values.
        /// When null numeric columns are right aligned and the others are left aligned.
        /// </summary>
        public PlainTextReportAlignment? Alignment { get; set; }

        /// <summary>
        /// Alignment of the header.
        /// When null the alignment of the values is used.
        /// </summary>
        public PlainTextReportAlignment? HeaderAlignment { get; set; }
    }
}

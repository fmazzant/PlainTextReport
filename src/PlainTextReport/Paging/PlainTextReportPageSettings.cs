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

    /// <summary>
    /// The page settings: when they are set on the options, the report is split in pages.
    /// The paper size is converted to characters and lines using <see cref="CharactersPerInch"/> and <see cref="LinesPerInch"/>.
    /// </summary>
    public sealed class PlainTextReportPageSettings
    {
        private const double MillimetersPerInch = 25.4;

        /// <summary>
        /// Create the page settings for A4 portrait
        /// </summary>
        public PlainTextReportPageSettings()
            : this(PaperSize.A4, PageOrientation.Portrait)
        {
        }

        /// <summary>
        /// Create the page settings
        /// </summary>
        /// <param name="paperSize"></param>
        /// <param name="orientation"></param>
        public PlainTextReportPageSettings(PaperSize paperSize, PageOrientation orientation = PageOrientation.Portrait)
        {
            PaperSize = paperSize ?? throw new ArgumentNullException(nameof(paperSize));
            Orientation = orientation;
        }

        /// <summary>
        /// Paper size
        /// </summary>
        public PaperSize PaperSize { get; set; }

        /// <summary>
        /// Page orientation
        /// </summary>
        public PageOrientation Orientation { get; set; }

        /// <summary>
        /// Characters per inch (pitch). 10 is pica, 12 is elite, 17 is condensed.
        /// </summary>
        public double CharactersPerInch { get; set; } = 10;

        /// <summary>
        /// Lines per inch. 6 is the common value, 8 is compressed.
        /// </summary>
        public double LinesPerInch { get; set; } = 6;

        /// <summary>
        /// Left margin, in characters
        /// </summary>
        public int MarginLeft { get; set; }

        /// <summary>
        /// Right margin, in characters
        /// </summary>
        public int MarginRight { get; set; }

        /// <summary>
        /// Top margin, in lines
        /// </summary>
        public int MarginTop { get; set; }

        /// <summary>
        /// Bottom margin, in lines
        /// </summary>
        public int MarginBottom { get; set; }

        /// <summary>
        /// Band printed at the top of every page
        /// </summary>
        public PlainTextReportPageBand Header { get; set; } = new PlainTextReportPageBand();

        /// <summary>
        /// Band printed at the bottom of every page
        /// </summary>
        public PlainTextReportPageBand Footer { get; set; } = new PlainTextReportPageBand();

        /// <summary>
        /// Repeats the column header on every page
        /// </summary>
        public bool RepeatColumnHeader { get; set; } = true;

        /// <summary>
        /// Text put at the beginning of every page after the first one by GetTextReport.
        /// The default is the form feed character.
        /// </summary>
        public string PageBreak { get; set; } = "\f";

        /// <summary>
        /// Date used by the {date} placeholder. When null the current date and time is used.
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        /// Page width, in characters (margins included)
        /// </summary>
        public int Width => ToUnits(PaperSize.GetWidthMillimeters(Orientation), CharactersPerInch);

        /// <summary>
        /// Page height, in lines (margins included)
        /// </summary>
        public int Height => ToUnits(PaperSize.GetHeightMillimeters(Orientation), LinesPerInch);

        /// <summary>
        /// Printable width, in characters
        /// </summary>
        public int PrintableWidth => Width - MarginLeft - MarginRight;

        /// <summary>
        /// Printable height, in lines
        /// </summary>
        public int PrintableHeight => Height - MarginTop - MarginBottom;

        private static int ToUnits(double millimeters, double unitsPerInch)
            => (int)Math.Floor(millimeters / MillimetersPerInch * unitsPerInch + 1e-9);
    }
}

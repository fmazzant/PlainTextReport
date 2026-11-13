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
    /// A paper size, in millimeters.
    /// The ISO 216 A series (A0 ... A10) is available with <see cref="A(int)"/> or with the static properties,
    /// the B series with <see cref="B(int)"/>, and the US sizes (Letter, Legal, Tabloid, Executive) and the
    /// continuous form paper with the static properties.
    /// </summary>
    public sealed class PaperSize
    {
        /// <summary>
        /// Name of the paper
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Length of the short side, in millimeters
        /// </summary>
        public double ShortSideMillimeters { get; }

        /// <summary>
        /// Length of the long side, in millimeters
        /// </summary>
        public double LongSideMillimeters { get; }

        private PaperSize(string name, double shortSide, double longSide)
        {
            Name = name;
            ShortSideMillimeters = shortSide;
            LongSideMillimeters = longSide;
        }

        /// <summary>
        /// Returns the width of the paper, in millimeters
        /// </summary>
        /// <param name="orientation"></param>
        /// <returns></returns>
        public double GetWidthMillimeters(PageOrientation orientation)
            => orientation == PageOrientation.Landscape ? LongSideMillimeters : ShortSideMillimeters;

        /// <summary>
        /// Returns the height of the paper, in millimeters
        /// </summary>
        /// <param name="orientation"></param>
        /// <returns></returns>
        public double GetHeightMillimeters(PageOrientation orientation)
            => orientation == PageOrientation.Landscape ? ShortSideMillimeters : LongSideMillimeters;

        /// <summary>
        /// Returns the ISO 216 A paper size: A0 is 841 x 1189 mm, every next size halves the long side.
        /// </summary>
        /// <param name="number">From 0 (A0) to 10 (A10)</param>
        /// <returns></returns>
        public static PaperSize A(int number)
        {
            if (number < 0 || number > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(number), "The A series goes from A0 to A10.");
            }

            int shortSide = 841, longSide = 1189;
            for (int i = 0; i < number; i++)
            {
                var half = longSide / 2;
                longSide = shortSide;
                shortSide = half;
            }
            return new PaperSize("A" + number.ToString(CultureInfo.InvariantCulture), shortSide, longSide);
        }

        /// <summary>
        /// Returns the ISO 216 B paper size: B0 is 1000 x 1414 mm, every next size halves the long side.
        /// </summary>
        /// <param name="number">From 0 (B0) to 10 (B10)</param>
        /// <returns></returns>
        public static PaperSize B(int number)
        {
            if (number < 0 || number > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(number), "The B series goes from B0 to B10.");
            }

            int shortSide = 1000, longSide = 1414;
            for (int i = 0; i < number; i++)
            {
                var half = longSide / 2;
                longSide = shortSide;
                shortSide = half;
            }
            return new PaperSize("B" + number.ToString(CultureInfo.InvariantCulture), shortSide, longSide);
        }

        /// <summary>
        /// The name of a custom paper size created without a name
        /// </summary>
        public const string CustomName = "Custom";

        /// <summary>
        /// Creates a custom paper size, in millimeters
        /// </summary>
        /// <param name="name">The name of the paper; when null or empty it is <see cref="CustomName"/></param>
        /// <param name="widthMillimeters"></param>
        /// <param name="heightMillimeters"></param>
        /// <returns></returns>
        public static PaperSize Custom(string name, double widthMillimeters, double heightMillimeters)
        {
            if (widthMillimeters <= 0) throw new ArgumentOutOfRangeException(nameof(widthMillimeters));
            if (heightMillimeters <= 0) throw new ArgumentOutOfRangeException(nameof(heightMillimeters));
            return new PaperSize(string.IsNullOrEmpty(name) ? CustomName : name, Math.Min(widthMillimeters, heightMillimeters), Math.Max(widthMillimeters, heightMillimeters));
        }

        /// <summary>A0, 841 x 1189 mm</summary>
        public static PaperSize A0 => A(0);
        /// <summary>A1, 594 x 841 mm</summary>
        public static PaperSize A1 => A(1);
        /// <summary>A2, 420 x 594 mm</summary>
        public static PaperSize A2 => A(2);
        /// <summary>A3, 297 x 420 mm</summary>
        public static PaperSize A3 => A(3);
        /// <summary>A4, 210 x 297 mm</summary>
        public static PaperSize A4 => A(4);
        /// <summary>A5, 148 x 210 mm</summary>
        public static PaperSize A5 => A(5);
        /// <summary>A6, 105 x 148 mm</summary>
        public static PaperSize A6 => A(6);
        /// <summary>A7, 74 x 105 mm</summary>
        public static PaperSize A7 => A(7);
        /// <summary>A8, 52 x 74 mm</summary>
        public static PaperSize A8 => A(8);
        /// <summary>A9, 37 x 52 mm</summary>
        public static PaperSize A9 => A(9);
        /// <summary>A10, 26 x 37 mm</summary>
        public static PaperSize A10 => A(10);

        /// <summary>B4, 250 x 353 mm</summary>
        public static PaperSize B4 => B(4);
        /// <summary>B5, 176 x 250 mm</summary>
        public static PaperSize B5 => B(5);

        /// <summary>US Letter, 8.5 x 11 in (215.9 x 279.4 mm)</summary>
        public static PaperSize Letter => new PaperSize("Letter", 215.9, 279.4);
        /// <summary>US Legal, 8.5 x 14 in (215.9 x 355.6 mm)</summary>
        public static PaperSize Legal => new PaperSize("Legal", 215.9, 355.6);
        /// <summary>US Tabloid (Ledger in landscape), 11 x 17 in (279.4 x 431.8 mm)</summary>
        public static PaperSize Tabloid => new PaperSize("Tabloid", 279.4, 431.8);
        /// <summary>US Executive, 7.25 x 10.5 in (184.15 x 266.7 mm)</summary>
        public static PaperSize Executive => new PaperSize("Executive", 184.15, 266.7);

        /// <summary>
        /// Continuous form paper for dot-matrix printers, 240 mm x 12 in (240 x 304.8 mm):
        /// the common fanfold paper with the tractor holes
        /// </summary>
        public static PaperSize ContinuousForm => new PaperSize("ContinuousForm", 240, 304.8);

        /// <summary>
        /// Returns the name and the size of the paper
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => string.Format(CultureInfo.InvariantCulture, "{0} ({1} x {2} mm)", Name, ShortSideMillimeters, LongSideMillimeters);
    }
}

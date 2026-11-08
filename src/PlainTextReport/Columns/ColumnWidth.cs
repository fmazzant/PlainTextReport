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
    /// The width of a column: a number of characters, or a percentage of the free space.
    /// <para>
    /// A percentage is a share of the space left by the other columns in the report width
    /// (<see cref="PlainTextReportOptions.Width"/> or the printable width of the page).
    /// When the percentages add up to more than 100% they are scaled down proportionally:
    /// two columns at 100% get 50% of the free space each.
    /// Without a report width a percentage column is sized like an automatic column.
    /// </para>
    /// It can be assigned from an int (<c>style.Width = 20</c>) or from a string (<c>style.Width = "50%"</c>).
    /// </summary>
    public readonly struct ColumnWidth : IEquatable<ColumnWidth>
    {
        private ColumnWidth(double value, bool isPercent)
        {
            Value = value;
            IsPercent = isPercent;
        }

        /// <summary>
        /// Number of characters, or the percentage when <see cref="IsPercent"/> is true
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// True when the width is a percentage of the free space
        /// </summary>
        public bool IsPercent { get; }

        /// <summary>
        /// A width in characters
        /// </summary>
        /// <param name="characters">At least 1</param>
        /// <returns></returns>
        public static ColumnWidth Characters(int characters)
        {
            if (characters < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(characters), "The width must be at least 1 character.");
            }
            return new ColumnWidth(characters, false);
        }

        /// <summary>
        /// A width as percentage of the free space
        /// </summary>
        /// <param name="percent">Greater than 0, for example 100 for 100%</param>
        /// <returns></returns>
        public static ColumnWidth Percent(double percent)
        {
            if (!(percent > 0) || double.IsInfinity(percent))
            {
                throw new ArgumentOutOfRangeException(nameof(percent), "The percentage must be greater than 0.");
            }
            return new ColumnWidth(percent, true);
        }

        /// <summary>
        /// Parses a width: "20" is 20 characters, "50%" is 50% of the free space
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static ColumnWidth Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var value = text.Trim();
            if (value.EndsWith("%", StringComparison.Ordinal))
            {
                var number = value.Substring(0, value.Length - 1).Trim();
                if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
                {
                    return Percent(percent);
                }
            }
            else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var characters))
            {
                return Characters(characters);
            }

            throw new FormatException($"'{text}' is not a valid column width: use a number of characters (\"20\") or a percentage (\"50%\").");
        }

        /// <summary>
        /// A width in characters
        /// </summary>
        /// <param name="characters"></param>
        public static implicit operator ColumnWidth(int characters) => Characters(characters);

        /// <summary>
        /// A width parsed from text, for example "20" or "50%"
        /// </summary>
        /// <param name="text"></param>
        public static implicit operator ColumnWidth(string text) => Parse(text);

        /// <summary>
        /// Equals
        /// </summary>
        public bool Equals(ColumnWidth other) => Value.Equals(other.Value) && IsPercent == other.IsPercent;

        /// <summary>
        /// Equals
        /// </summary>
        public override bool Equals(object obj) => obj is ColumnWidth other && Equals(other);

        /// <summary>
        /// Hash code
        /// </summary>
        public override int GetHashCode() => Value.GetHashCode() * 2 + (IsPercent ? 1 : 0);

        /// <summary>
        /// Equality
        /// </summary>
        public static bool operator ==(ColumnWidth left, ColumnWidth right) => left.Equals(right);

        /// <summary>
        /// Inequality
        /// </summary>
        public static bool operator !=(ColumnWidth left, ColumnWidth right) => !left.Equals(right);

        /// <summary>
        /// Returns "20" or "50%"
        /// </summary>
        public override string ToString()
            => Value.ToString(CultureInfo.InvariantCulture) + (IsPercent ? "%" : string.Empty);
    }
}

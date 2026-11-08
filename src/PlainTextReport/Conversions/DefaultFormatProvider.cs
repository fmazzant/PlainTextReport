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

namespace Mafesoft.PlainTextReport.Conversions
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Formats the values with the format of the column, in the culture of the report
    /// </summary>
    internal sealed class DefaultFormatProvider : IFormatProvider, ICustomFormatter
    {
        /// <summary>
        /// Column's format
        /// </summary>
        public string CustomFormat { get; }

        /// <summary>
        /// The culture of the values
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Create Default Format Provider
        /// </summary>
        /// <param name="customFormat">The format of the column, for example N2 or dd/MM/yyyy</param>
        /// <param name="culture">The culture; the invariant culture when null</param>
        public DefaultFormatProvider(string customFormat, CultureInfo culture = null)
        {
            CustomFormat = customFormat;
            Culture = culture ?? CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// Returns the formatter for string.Format and the culture's formats for the other requests
        /// </summary>
        /// <param name="formatType"></param>
        /// <returns></returns>
        public object GetFormat(Type formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : Culture.GetFormat(formatType);
        }

        /// <summary>
        /// Formats the value with the format of the column
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg is IFormattable formattable)
            {
                return formattable.ToString(CustomFormat, Culture);
            }
            return arg?.ToString() ?? string.Empty;
        }
    }
}

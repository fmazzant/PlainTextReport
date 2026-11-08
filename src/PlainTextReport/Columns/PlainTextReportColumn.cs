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
    using System.Linq.Expressions;
    using Mafesoft.PlainTextReport.Conversions;

    /// <summary>
    /// The column definition.
    /// </summary>
    public sealed class PlainTextReportColumn
    {
        /// <summary>
        /// Column's index
        /// </summary>
        public int ColumnIndex { get; internal set; }

        /// <summary>
        /// Column's name
        /// </summary>
        public string ColumnName { get; internal set; }

        /// <summary>
        /// The compiled getter of the value: a Func&lt;M, object&gt; of the model type
        /// </summary>
        internal Delegate Getter { get; set; }

        /// <summary>
        /// Column's type
        /// </summary>
        public Type ColumnType { get; internal set; }

        /// <summary>
        /// Column expression
        /// </summary>
        public Expression ColumnExpression { get; internal set; }

        /// <summary>
        /// Column's value format
        /// </summary>
        public string ColumnFormat { get; internal set; }

        /// <summary>
        /// Converter
        /// </summary>
        public IValueConverter Converter { get; internal set; }

        /// <summary>
        /// Value format provider
        /// </summary>
        private IFormatProvider _FormatProvider = null;

        /// <summary>
        /// Column's value format provider. When it is not set, the values are formatted with
        /// <see cref="ColumnFormat"/> in the culture of the report (<see cref="PlainTextReportOptions.Culture"/>);
        /// read from here, without a report, the culture is the invariant one.
        /// </summary>
        public IFormatProvider ColumnFormatProvider
        {
            get
            {
                return GetFormatProvider(CultureInfo.InvariantCulture);
            }
            set
            {
                _FormatProvider = value;
            }
        }

        /// <summary>
        /// Returns the format provider of the values, for the culture of the report
        /// </summary>
        /// <param name="culture">The culture of the report</param>
        /// <returns></returns>
        internal IFormatProvider GetFormatProvider(CultureInfo culture)
        {
            if (_FormatProvider != null)
            {
                return _FormatProvider;
            }
            if (string.IsNullOrEmpty(ColumnFormat))
            {
                return culture;
            }
            return new DefaultFormatProvider(ColumnFormat, culture);
        }

        /// <summary>
        /// Column's style
        /// </summary>
        public PlainTextReportColumnStyle Style { get; set; } = new PlainTextReportColumnStyle();

        /// <summary>
        /// The text printed in the header: <see cref="PlainTextReportColumnStyle.Caption"/> when set, otherwise <see cref="ColumnName"/>
        /// </summary>
        public string Caption => Style?.Caption ?? ColumnName;
    }
}

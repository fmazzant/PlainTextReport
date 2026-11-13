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
        /// Column's index: the columns are printed in index order. Two columns can have the same index:
        /// they keep the order they were added in. The AddColumn overloads without an index use the number
        /// of columns already added, so mixing them with explicit indexes can give duplicates.
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
        /// True when the converter is the default one of the column type, not a custom one
        /// </summary>
        internal bool HasDefaultConverter { get; set; }

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

        private PlainTextReportColumnStyle _Style = new PlainTextReportColumnStyle();

        /// <summary>
        /// Column's style. It cannot be null.
        /// </summary>
        /// <exception cref="ArgumentNullException">The style is null</exception>
        public PlainTextReportColumnStyle Style
        {
            get => _Style;
            set => _Style = value ?? throw new ArgumentNullException(nameof(value), $"The style of column '{ColumnName}' cannot be null.");
        }

        /// <summary>
        /// The text printed in the header: <see cref="PlainTextReportColumnStyle.Caption"/> when set, otherwise <see cref="ColumnName"/>
        /// </summary>
        public string Caption => Style?.Caption ?? ColumnName;
    }
}

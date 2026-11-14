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
    using Mafesoft.PlainTextReport.Conversions;
    using Mafesoft.PlainTextReport.Extensions;
    using Mafesoft.PlainTextReport.Factory;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// PlainTextReport Options Collection Columns
    /// </summary>
    /// <typeparam name="M">The model type</typeparam>
    public sealed class PlainTextReportOptionsColumns<M> : IEnumerable<PlainTextReportColumn>
    {
        /// <summary>
        /// The message of the obsolete AddColumn overloads
        /// </summary>
        private const string ObsoleteAddColumn = "Use Columns.Add(m => m.Property, column => column.Caption(...).Format(...)) instead. The AddColumn overloads will be removed in 2.0.";

        /// <summary>
        /// Columns
        /// </summary>
        private List<PlainTextReportColumn> Columns { get; set; }

        /// <summary>
        /// Number of Columns
        /// </summary>
        public int Count => Columns.Count;

        /// <summary>
        /// Create PlainTextReportOptionColumns
        /// </summary>
        public PlainTextReportOptionsColumns()
        {
            Columns = new List<PlainTextReportColumn>();
        }

        /// <summary>
        /// Add a column, with every setting in one place:
        /// <code>
        /// options.Columns.Add(m => m.Price, column => column
        ///     .Caption("Price")
        ///     .Format("N2")
        ///     .Width(10)
        ///     .Align(PlainTextReportAlignment.Right)
        ///     .Converter(new EuroConverter())
        ///     .Index(2));
        /// </code>
        /// The name of the column is the name of the property: "Price", or "Customer.Name" for m =&gt; m.Customer.Name.
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="expression">The property or field of the model, also nested: m =&gt; m.Customer.Name</param>
        /// <param name="column">Sets the column: caption, index, format, format provider, converter, width, alignment, multi-line, null text</param>
        /// <exception cref="ArgumentNullException">The expression is null</exception>
        /// <exception cref="ArgumentException">The expression does not select a property or a field of the model</exception>
        public void Add<T>(Expression<Func<M, T>> expression, Action<PlainTextReportColumnBuilder> column = null)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            this.Add(expression.GetPropertyName(), expression, column);
        }

        /// <summary>
        /// Add a column with the given name, with every setting in one place:
        /// <code>
        /// options.Columns.Add("Product", m => m.Name, column => column.Width(20));
        /// </code>
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="columnName">The name of the column, printed in the header when there is no caption; null for the name of the property</param>
        /// <param name="expression">The property or field of the model, also nested: m =&gt; m.Customer.Name</param>
        /// <param name="column">Sets the column: caption, index, format, format provider, converter, width, alignment, multi-line, null text</param>
        /// <exception cref="ArgumentNullException">The expression is null</exception>
        /// <exception cref="ArgumentException">The expression does not select a property or a field of the model</exception>
        public void Add<T>(string columnName, Expression<Func<M, T>> expression, Action<PlainTextReportColumnBuilder> column = null)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var created = this.Create(Columns.Count, columnName ?? expression.GetPropertyName(), expression, null, null, null);
            column?.Invoke(new PlainTextReportColumnBuilder(created));
            Columns.Add(created);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="columnIndex">The position of the column: the columns are printed in index order; columns with the same index keep the order they were added in</param>
        /// <param name="expression">The property or field of the model, also nested: m =&gt; m.Customer.Name</param>
        /// <param name="columnFormat">The format of the values, for example N2 or dd/MM/yyyy</param>
        [Obsolete(ObsoleteAddColumn)]
        public void AddColumn<T>(int columnIndex, Expression<Func<M, T>> expression, string columnFormat = null)
        {
            var name = expression.GetPropertyName();
            this.AddColumn(columnIndex, name, expression, columnFormat);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="columnName">The name of the column, printed in the header</param>
        /// <param name="expression">The property or field of the model, also nested: m =&gt; m.Customer.Name</param>
        /// <param name="columnFormat">The format of the values, for example N2 or dd/MM/yyyy</param>
        [Obsolete(ObsoleteAddColumn)]
        public void AddColumn<T>(string columnName, Expression<Func<M, T>> expression, string columnFormat = null)
        {
            this.AddColumn(Columns.Count, columnName, expression, columnFormat);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="expression">The property or field of the model, also nested: m =&gt; m.Customer.Name</param>
        /// <param name="style">Sets the style of the column: caption, width, alignment, multi-line</param>
        /// <param name="columnFormat">The format of the values, for example N2 or dd/MM/yyyy</param>
        [Obsolete(ObsoleteAddColumn)]
        public void AddColumn<T>(Expression<Func<M, T>> expression, Action<PlainTextReportColumnStyle> style, string columnFormat = null)
        {
            var columnName = expression.GetPropertyName();
            var index = Columns.Count;
            this.AddColumn(index, columnName, expression, columnFormat);
            style(Columns[index].Style);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="columnIndex">The position of the column: the columns are printed in index order; columns with the same index keep the order they were added in</param>
        /// <param name="columnName">The name of the column, printed in the header</param>
        /// <param name="expression">The property or field of the model, also nested: m =&gt; m.Customer.Name</param>
        /// <param name="formatProvider">The format provider of the values; it wins over the culture of the report</param>
        [Obsolete(ObsoleteAddColumn)]
        public void AddColumn<T>(int columnIndex, string columnName, Expression<Func<M, T>> expression, IFormatProvider formatProvider)
        {
            this.AddColumn(columnIndex, columnName, expression, null, formatProvider);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="columnIndex">The position of the column: the columns are printed in index order; columns with the same index keep the order they were added in</param>
        /// <param name="columnName">The name of the column, printed in the header</param>
        /// <param name="expression">The property or field of the model, also nested: m =&gt; m.Customer.Name</param>
        /// <param name="converter">Converts the values to text</param>
        [Obsolete(ObsoleteAddColumn)]
        public void AddColumn<T>(int columnIndex, string columnName, Expression<Func<M, T>> expression, IValueConverter converter)
        {
            this.AddColumn(columnIndex, columnName, expression, null, null, converter);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="expression">The property or field of the model, also nested: m =&gt; m.Customer.Name</param>
        /// <param name="formatProvider">The format provider of the values; it wins over the culture of the report</param>
        [Obsolete(ObsoleteAddColumn)]
        public void AddColumn<T>(Expression<Func<M, T>> expression, IFormatProvider formatProvider)
        {
            var columnName = expression.GetPropertyName();
            this.AddColumn(Columns.Count, columnName, expression, null, formatProvider);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="expression">The property or field of the model, also nested: m =&gt; m.Customer.Name</param>
        /// <param name="style">Sets the style of the column: caption, width, alignment, multi-line</param>
        /// <param name="converter">Converts the values to text</param>
        [Obsolete(ObsoleteAddColumn)]
        public void AddColumn<T>(Expression<Func<M, T>> expression, Action<PlainTextReportColumnStyle> style, IValueConverter converter)
        {
            var columnName = expression.GetPropertyName();
            var index = Columns.Count;
            this.AddColumn(index, columnName, expression, null, null, converter);
            style(Columns[index].Style);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="columnName">The name of the column, printed in the header</param>
        /// <param name="expression">The property or field of the model, also nested: m =&gt; m.Customer.Name</param>
        /// <param name="converter">Converts the values to text</param>
        [Obsolete(ObsoleteAddColumn)]
        public void AddColumn<T>(string columnName, Expression<Func<M, T>> expression, IValueConverter converter)
        {
            this.AddColumn(Columns.Count, columnName, expression, null, null, converter);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="columnIndex">The position of the column: the columns are printed in index order; columns with the same index keep the order they were added in</param>
        /// <param name="columnName">The name of the column, printed in the header</param>
        /// <param name="expression">The property or field of the model, also nested: m =&gt; m.Customer.Name</param>
        /// <param name="columnFormat">The format of the values, for example N2 or dd/MM/yyyy</param>
        /// <param name="formatProvider">The format provider of the values; it wins over the culture of the report</param>
        /// <param name="valueConverter">Converts the values to text</param>
        private void AddColumn<T>(int columnIndex, string columnName, Expression<Func<M, T>> expression, string columnFormat = null, IFormatProvider formatProvider = null, IValueConverter valueConverter = null)
        {
            Columns.Add(this.Create(columnIndex, columnName, expression, columnFormat, formatProvider, valueConverter));
        }

        /// <summary>
        /// Creates a column, without adding it
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="columnIndex">The position of the column; a negative index is the number of columns already added</param>
        /// <param name="columnName">The name of the column, printed in the header</param>
        /// <param name="expression">The property or field of the model, also nested: m =&gt; m.Customer.Name</param>
        /// <param name="columnFormat">The format of the values, for example N2 or dd/MM/yyyy</param>
        /// <param name="formatProvider">The format provider of the values; it wins over the culture of the report</param>
        /// <param name="valueConverter">Converts the values to text</param>
        private PlainTextReportColumn Create<T>(int columnIndex, string columnName, Expression<Func<M, T>> expression, string columnFormat, IFormatProvider formatProvider, IValueConverter valueConverter)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var type = typeof(T);
            var index = columnIndex < 0 ? Columns.Count : columnIndex;
            var converter = valueConverter ?? ValueConverterFactory.CreateValueConverterByType(type);

            return new PlainTextReportColumn()
            {
                ColumnIndex = index,
                ColumnName = columnName,
                Getter = expression.CreateGetter<M>(),
                ColumnType = type,
                ColumnExpression = expression,
                ColumnFormat = columnFormat,
                ColumnFormatProvider = formatProvider,
                Converter = converter,
                HasDefaultConverter = valueConverter == null
            };
        }

        /// <summary>
        /// Get columns
        /// </summary>
        /// <returns></returns>
        public IEnumerator<PlainTextReportColumn> GetEnumerator() => Columns.GetEnumerator();

        /// <summary>
        /// Get columns
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();


    }
}

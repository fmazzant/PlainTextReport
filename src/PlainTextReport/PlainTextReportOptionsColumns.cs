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

namespace PlainTextReport
{
    using PlainTextReport.Conversions;
    using PlainTextReport.Extensions;
    using PlainTextReport.Factory;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// PlainTextReport Options Collection Columns
    /// </summary>
    /// <typeparam name="M"></typeparam>
    public sealed class PlainTextReportOptionsColumns<M> : IEnumerable<PlainTextReportColumn>
    {
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
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnIndex"></param>
        /// <param name="expression"></param>
        /// <param name="columnFormat"></param>
        public void AddColumn<T>(int columnIndex, Expression<Func<M, T>> expression, string columnFormat = null)
        {
            var name = expression.GetPropertyName();
            this.AddColumn(columnIndex, name, expression, columnFormat);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnName"></param>
        /// <param name="expression"></param>
        /// <param name="columnFormat"></param>
        public void AddColumn<T>(string columnName, Expression<Func<M, T>> expression, string columnFormat = null)
        {
            this.AddColumn(Columns.Count, columnName, expression, columnFormat);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <param name="columnFormat"></param>
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
        /// <typeparam name="T"></typeparam>
        /// <param name="columnIndex"></param>
        /// <param name="columnName"></param>
        /// <param name="expression"></param>
        /// <param name="formatProvider"></param>
        public void AddColumn<T>(int columnIndex, string columnName, Expression<Func<M, T>> expression, IFormatProvider formatProvider)
        {
            this.AddColumn(columnIndex, columnName, expression, null, formatProvider);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnIndex"></param>
        /// <param name="columnName"></param>
        /// <param name="expression"></param>
        /// <param name="converter"></param>
        public void AddColumn<T>(int columnIndex, string columnName, Expression<Func<M, T>> expression, IValueConverter converter)
        {
            this.AddColumn(columnIndex, columnName, expression, null, null, converter);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <param name="formatProvider"></param>
        public void AddColumn<T>(Expression<Func<M, T>> expression, IFormatProvider formatProvider)
        {
            var columnName = expression.GetPropertyName();
            this.AddColumn(Columns.Count, columnName, expression, null, formatProvider);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <param name=""></param>
        /// <param name="converter"></param>
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
        /// <typeparam name="T"></typeparam>
        /// <param name="columnName"></param>
        /// <param name="expression"></param>
        /// <param name=""></param>
        /// <param name="converter"></param>
        public void AddColumn<T>(string columnName, Expression<Func<M, T>> expression, IValueConverter converter)
        {
            this.AddColumn(Columns.Count, columnName, expression, null, null, converter);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnIndex"></param>
        /// <param name="columnName"></param>
        /// <param name="expression"></param>
        /// <param name="columnFormat"></param>
        /// <param name="formatProvider"></param>
        private void AddColumn<T>(int columnIndex, string columnName, Expression<Func<M, T>> expression, string columnFormat = null, IFormatProvider formatProvider = null, IValueConverter converter = null)
        {
            AddColumn(typeof(T), columnIndex, columnName, expression, columnFormat, formatProvider, converter);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="columnName"></param>
        /// <param name="expression"></param>
        /// <param name="columnFormat"></param>
        /// <param name="formatProvider"></param>
        private void AddColumn(Type type, int columnIndex, string columnName, Expression expression, string columnFormat = null, IFormatProvider formatProvider = null, IValueConverter valueConverter = null)
        {
            var index = columnIndex < 0 ? Columns.Count : columnIndex;
            var columnNameInternal = expression?.GetPropertyName() ?? columnName;
            var converter = valueConverter ?? ValueConverterFactory.CreateValueConverterByType(type);

            Columns.Add(new PlainTextReportColumn()
            {
                ColumnIndex = index,
                ColumnName = columnName,
                ColumnNameInternal = columnNameInternal,
                ColumnType = type,
                ColumnExpression = expression,
                ColumnFormat = columnFormat,
                ColumnFormatProvider = formatProvider,
                Converter = converter
            });
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

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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Numerics;

    /// <summary>
    /// Computes the aggregate of a column while the rows are read, from the values before they are converted to text
    /// </summary>
    internal sealed class ColumnTotal
    {
        private readonly bool isDouble;
        private readonly bool isBigInteger;
        private long count;
        private decimal decimalSum;
        private double doubleSum;
        private BigInteger bigIntegerSum;
        private object min;
        private object max;

        /// <summary>
        /// Create the aggregate of a column
        /// </summary>
        /// <param name="kind">The aggregate</param>
        /// <param name="type">The type of the values</param>
        public ColumnTotal(PlainTextReportTotal kind, Type type)
        {
            Kind = kind;
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            isDouble = underlying == typeof(double) || underlying == typeof(float);
            isBigInteger = underlying == typeof(BigInteger);
        }

        /// <summary>
        /// The aggregate
        /// </summary>
        public PlainTextReportTotal Kind { get; }

        /// <summary>
        /// Adds a value; null values are skipped
        /// </summary>
        /// <exception cref="OverflowException">The sum is too large</exception>
        public void Add(object value)
        {
            if (value == null || Kind == PlainTextReportTotal.None)
            {
                return;
            }

            count++;
            switch (Kind)
            {
                case PlainTextReportTotal.Sum:
                case PlainTextReportTotal.Average:
                    if (isDouble) doubleSum += Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    else if (isBigInteger) bigIntegerSum += (BigInteger)value;
                    else decimalSum += Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    break;
                case PlainTextReportTotal.Min:
                    if (min == null || Comparer<object>.Default.Compare(value, min) < 0) min = value;
                    break;
                case PlainTextReportTotal.Max:
                    if (max == null || Comparer<object>.Default.Compare(value, max) > 0) max = value;
                    break;
            }
        }

        /// <summary>
        /// The aggregate of the values added so far: null for the average, the minimum and the maximum of no values
        /// </summary>
        /// <exception cref="OverflowException">The average of a BigInteger column does not fit a decimal</exception>
        public object Result
        {
            get
            {
                switch (Kind)
                {
                    case PlainTextReportTotal.Sum:
                        return isDouble ? doubleSum : isBigInteger ? (object)bigIntegerSum : decimalSum;
                    case PlainTextReportTotal.Count:
                        return count;
                    case PlainTextReportTotal.Average:
                        if (count == 0) return null;
                        if (isDouble) return doubleSum / count;
                        return (isBigInteger ? (decimal)bigIntegerSum : decimalSum) / count;
                    case PlainTextReportTotal.Min:
                        return min;
                    case PlainTextReportTotal.Max:
                        return max;
                    default:
                        return null;
                }
            }
        }
    }
}

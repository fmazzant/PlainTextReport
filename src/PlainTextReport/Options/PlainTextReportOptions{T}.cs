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
    /// PlainTextReport Options definition
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class PlainTextReportOptions<T> : PlainTextReportOptions
    {
        /// <summary>
        /// Columns
        /// </summary>
        public PlainTextReportOptionsColumns<T> Columns { get; internal set; }

        /// <summary>
        /// Create a PlainTextReportOptions
        /// </summary>
        public PlainTextReportOptions()
        {
            Columns = new PlainTextReportOptionsColumns<T>();
        }

        /// <summary>
        /// The settings of the groups; null when the report is not grouped. See <see cref="GroupBy{TKey}"/>.
        /// </summary>
        public PlainTextReportGroup Group { get; private set; }

        /// <summary>
        /// Returns the key of the group of an item; null when the report is not grouped
        /// </summary>
        internal Func<T, object> GroupKey { get; private set; }

        /// <summary>
        /// Groups the rows by a key: a new group starts every time the key changes, with a header above it and,
        /// optionally, a subtotals row below it. The groups follow the order of the collection, which is not sorted:
        /// sort it by the key first, otherwise the same key can make more groups. A new call replaces the grouping.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <param name="key">Returns the key of the group of an item</param>
        /// <param name="configure">Sets the header, the subtotals and the page breaks of the groups</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        public void GroupBy<TKey>(Func<T, TKey> key, Action<PlainTextReportGroup> configure = null)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var group = new PlainTextReportGroup();
            configure?.Invoke(group);
            Group = group;
            GroupKey = model => key(model);
        }
    }
}

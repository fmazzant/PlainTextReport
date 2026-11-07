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

namespace Mafesoft.PlainTextReport
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// IPlainTextReport interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPlainTextReport<T> where T : class
    {
        /// <summary>
        /// Bind the collection
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        IPlainTextReport<T> Bind(IEnumerable<T> values);

        /// <summary>
        /// Generate text report
        /// </summary>
        /// <returns></returns>
        string GetTextReport();

        /// <summary>
        /// Generate text line report
        /// </summary>
        /// <returns></returns>
        string[] GetTextLinesReport();

        /// <summary>
        /// Generate the report split in pages
        /// </summary>
        /// <returns></returns>
        PlainTextReportPage[] GetPagesReport();

        /// <summary>
        /// Saves the whole report in a file, with the page breaks between the pages
        /// </summary>
        /// <param name="path">The file path, absolute or relative to the current directory; the file is overwritten
        /// and its directory is created when missing</param>
        /// <param name="encoding">The encoding; UTF-8 without BOM by default</param>
        /// <returns>The absolute path of the file</returns>
        string SaveToFile(string path, Encoding encoding = null);

        /// <summary>
        /// Saves every page in its own file
        /// </summary>
        /// <param name="directory">The directory, absolute or relative to the current directory; it is created when missing</param>
        /// <param name="fileNamePattern">The file name, with the placeholders {page}, {pages}, {title} and {date}</param>
        /// <param name="encoding">The encoding; UTF-8 without BOM by default</param>
        /// <returns>The absolute paths of the files, in page order</returns>
        string[] SavePagesToFiles(string directory, string fileNamePattern = "page-{page:000}.txt", Encoding encoding = null);
    }
}

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
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// PlainTextReport generates a plain text report from a collection of objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class PlainTextReport<T> : IPlainTextReport<T> where T : class
    {
        /// <summary>
        /// Options
        /// </summary>
        internal PlainTextReportOptions<T> Options { get; private set; }

        /// <summary>
        /// Collection
        /// </summary>
        public IEnumerable<T> Values { get; private set; }

        /// <summary>
        /// Create a new instance of PlainTextReport
        /// </summary>
        /// <param name="options"></param>
        public PlainTextReport(Action<PlainTextReportOptions<T>> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            Options = new PlainTextReportOptions<T>();
            options(Options);
        }

        /// <summary>
        /// Create a new instance of PlainTextReport
        /// </summary>
        /// <param name="options"></param>
        public PlainTextReport(PlainTextReportOptions<T> options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Bind the collection
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public IPlainTextReport<T> Bind(IEnumerable<T> values)
        {
            Values = values;
            return this;
        }

        /// <summary>
        /// Generate text report. When the page settings are set, every page after the first one
        /// starts with <see cref="PlainTextReportPageSettings.PageBreak"/> (form feed by default).
        /// </summary>
        /// <returns></returns>
        public string GetTextReport()
        {
            var pages = this.GetPagesReport();
            var pageBreak = Options.Page?.PageBreak ?? string.Empty;
            return string.Join(Environment.NewLine + pageBreak, pages.Select(page => page.ToString()));
        }

        /// <summary>
        /// Generate text line report. The lines of all the pages, without page breaks.
        /// </summary>
        /// <returns></returns>
        public string[] GetTextLinesReport()
        {
            return new PlainTextReportRenderer<T>(Options).Render(Values);
        }

        /// <summary>
        /// Generate the report split in pages. Without page settings the report is a single page.
        /// </summary>
        /// <returns></returns>
        public PlainTextReportPage[] GetPagesReport()
        {
            return new PlainTextReportRenderer<T>(Options).RenderPages(Values);
        }

        /// <summary>
        /// The default file name of the pages: page-001.txt, page-002.txt, ...
        /// </summary>
        public const string DefaultPageFileNamePattern = "page-{page:000}.txt";

        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);

        private const int BufferSize = 4096;

        #region Write

        /// <summary>
        /// Writes the whole report, with the page breaks between the pages and a final line break
        /// </summary>
        /// <param name="writer">The writer; it is not closed</param>
        public void WriteTo(TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            foreach (var chunk in GetChunks(GetPagesReport()))
            {
                writer.Write(chunk);
            }
            writer.Flush();
        }

        /// <summary>
        /// Writes the whole report, with the page breaks between the pages and a final line break
        /// </summary>
        /// <param name="writer">The writer; it is not closed</param>
        /// <param name="cancellationToken">Stops the writing between two pages</param>
        public async Task WriteToAsync(TextWriter writer, CancellationToken cancellationToken = default)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            foreach (var chunk in GetChunks(GetPagesReport()))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WriteAsync(chunk).ConfigureAwait(false);
            }
            await writer.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Writes the whole report in a stream, for example a <see cref="MemoryStream"/> or the body of an HTTP response
        /// </summary>
        /// <param name="stream">The stream; it is not closed</param>
        /// <param name="encoding">The encoding; UTF-8 without BOM by default. The BOM of the encoding, if any,
        /// is written only at the beginning of a seekable stream.</param>
        public void WriteTo(Stream stream, Encoding encoding = null)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            using (var writer = new StreamWriter(stream, encoding ?? DefaultEncoding, BufferSize, leaveOpen: true))
            {
                WriteTo(writer);
            }
        }

        /// <summary>
        /// Writes the whole report in a stream, for example a <see cref="MemoryStream"/> or the body of an HTTP response
        /// </summary>
        /// <param name="stream">The stream; it is not closed</param>
        /// <param name="encoding">The encoding; UTF-8 without BOM by default. The BOM of the encoding, if any,
        /// is written only at the beginning of a seekable stream.</param>
        /// <param name="cancellationToken">Stops the writing between two pages</param>
        public async Task WriteToAsync(Stream stream, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            using (var writer = new StreamWriter(stream, encoding ?? DefaultEncoding, BufferSize, leaveOpen: true))
            {
                await WriteToAsync(writer, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns the text of the report a page at a time: every page after the first one
        /// starts with the page break, and the report ends with a line break
        /// </summary>
        private IEnumerable<string> GetChunks(PlainTextReportPage[] pages)
        {
            var pageBreak = Options.Page?.PageBreak ?? string.Empty;
            for (int i = 0; i < pages.Length; i++)
            {
                var text = pages[i].ToString();
                yield return i == 0 ? text : Environment.NewLine + pageBreak + text;
            }
            yield return Environment.NewLine;
        }

        #endregion

        #region Save

        /// <summary>
        /// Saves the whole report in a file, with the page breaks between the pages
        /// </summary>
        /// <param name="path">The file path, absolute or relative to the current directory; the file is overwritten
        /// and its directory is created when missing</param>
        /// <param name="encoding">The encoding; UTF-8 without BOM by default</param>
        /// <returns>The absolute path of the file</returns>
        public string SaveToFile(string path, Encoding encoding = null)
        {
            var fullPath = PrepareFile(path);
            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize))
            {
                WriteTo(stream, encoding);
            }
            return fullPath;
        }

        /// <summary>
        /// Saves the whole report in a file, with the page breaks between the pages
        /// </summary>
        /// <param name="path">The file path, absolute or relative to the current directory; the file is overwritten
        /// and its directory is created when missing</param>
        /// <param name="encoding">The encoding; UTF-8 without BOM by default</param>
        /// <param name="cancellationToken">Stops the writing between two pages</param>
        /// <returns>The absolute path of the file</returns>
        public async Task<string> SaveToFileAsync(string path, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            var fullPath = PrepareFile(path);
            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
            {
                await WriteToAsync(stream, encoding, cancellationToken).ConfigureAwait(false);
            }
            return fullPath;
        }

        /// <summary>
        /// Saves every page in its own file
        /// </summary>
        /// <param name="directory">The directory, absolute or relative to the current directory; it is created when missing</param>
        /// <param name="fileNamePattern">The file name, with the placeholders {page}, {pages}, {title} and {date}</param>
        /// <param name="encoding">The encoding; UTF-8 without BOM by default</param>
        /// <returns>The absolute paths of the files, in page order</returns>
        public string[] SavePagesToFiles(string directory, string fileNamePattern = DefaultPageFileNamePattern, Encoding encoding = null)
        {
            var pages = GetPagesReport();
            var paths = PreparePageFiles(directory, fileNamePattern, pages);
            for (int i = 0; i < pages.Length; i++)
            {
                File.WriteAllText(paths[i], pages[i] + Environment.NewLine, encoding ?? DefaultEncoding);
            }
            return paths;
        }

        /// <summary>
        /// Saves every page in its own file
        /// </summary>
        /// <param name="directory">The directory, absolute or relative to the current directory; it is created when missing</param>
        /// <param name="fileNamePattern">The file name, with the placeholders {page}, {pages}, {title} and {date}</param>
        /// <param name="encoding">The encoding; UTF-8 without BOM by default</param>
        /// <param name="cancellationToken">Stops the writing between two files</param>
        /// <returns>The absolute paths of the files, in page order</returns>
        public async Task<string[]> SavePagesToFilesAsync(string directory, string fileNamePattern = DefaultPageFileNamePattern, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            var pages = GetPagesReport();
            var paths = PreparePageFiles(directory, fileNamePattern, pages);
            for (int i = 0; i < pages.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var stream = new FileStream(paths[i], FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
                using (var writer = new StreamWriter(stream, encoding ?? DefaultEncoding, BufferSize))
                {
                    await writer.WriteAsync(pages[i] + Environment.NewLine).ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            }
            return paths;
        }

        /// <summary>
        /// Checks the path, creates its directory and returns the absolute path
        /// </summary>
        private static string PrepareFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("The path is required.", nameof(path));
            }

            var fullPath = Path.GetFullPath(path);
            if (string.IsNullOrEmpty(Path.GetFileName(fullPath)))
            {
                throw new ArgumentException("The path must contain a file name.", nameof(path));
            }

            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return fullPath;
        }

        /// <summary>
        /// Checks the arguments, creates the directory and returns the absolute path of every page
        /// </summary>
        private string[] PreparePageFiles(string directory, string fileNamePattern, PlainTextReportPage[] pages)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("The directory is required.", nameof(directory));
            }
            if (string.IsNullOrWhiteSpace(fileNamePattern))
            {
                throw new ArgumentException("The file name pattern is required.", nameof(fileNamePattern));
            }

            directory = Path.GetFullPath(directory);
            var date = Options.Page?.Date ?? DateTime.Now;
            var title = Options.IsMultiLineTitle ? string.Join(" ", Options.MultiLineTitle) : Options.Title ?? string.Empty;
            var culture = Options.Culture ?? CultureInfo.InvariantCulture;
            var invalidChars = Path.GetInvalidFileNameChars();

            var paths = pages.Select(page =>
            {
                var name = PlainTextReportRenderer<T>.Format(fileNamePattern, page.Number, page.Count, date, title, culture);
                name = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
                return Path.Combine(directory, name);
            }).ToArray();

            if (paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != paths.Length)
            {
                throw new ArgumentException("The file name pattern must give a different name to every page: use the {page} placeholder.", nameof(fileNamePattern));
            }

            Directory.CreateDirectory(directory);
            return paths;
        }

        #endregion
    }
}

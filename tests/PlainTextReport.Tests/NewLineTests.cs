using System.Text;
using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class NewLineTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "PlainTextReportTests", Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static IPlainTextReport<Item> CreateReport(string? newLine, bool paged = true)
        {
            // 20 characters x 12 lines: 3 pages for 25 items
            var page = new PlainTextReportPageSettings(PaperSize.Custom("Test", 25.4 * 2, 25.4 * 3))
            {
                CharactersPerInch = 10,
                LinesPerInch = 4,
                Date = new DateTime(2026, 11, 7),
            };
            return new PlainTextReport<Item>(options =>
            {
                options.Title = "Report";
                options.ShowHeader = true;
                options.Page = paged ? page : null;
                options.NewLine = newLine;
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(m => m.Name, style => { });
            }).Bind(Enumerable.Range(1, 25).Select(i => new Item { Id = i, Name = $"N{i}" }));
        }

        /// <summary>
        /// Checks that every line break of the text is the given one
        /// </summary>
        private static void AssertLineBreaks(string newLine, string text)
        {
            Assert.Contains(newLine, text);
            var rest = text.Replace(newLine, string.Empty);
            Assert.DoesNotContain("\n", rest);
            Assert.DoesNotContain("\r", rest);
        }

        [Theory]
        [InlineData("\n", true)]
        [InlineData("\r\n", true)]
        [InlineData("\n", false)]
        [InlineData("\r\n", false)]
        public void GetTextReport_uses_the_new_line(string newLine, bool paged)
        {
            var text = CreateReport(newLine, paged).GetTextReport();

            AssertLineBreaks(newLine, text);
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void Pages_use_the_new_line(string newLine)
        {
            var pages = CreateReport(newLine).GetPagesReport();

            Assert.All(pages, page => Assert.Equal(string.Join(newLine, page.Lines), page.ToString()));
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public async Task WriteTo_uses_the_new_line(string newLine)
        {
            var report = CreateReport(newLine);
            var expected = report.GetTextReport() + newLine;

            var writer = new StringWriter();
            report.WriteTo(writer);
            var asyncWriter = new StringWriter();
            await report.WriteToAsync(asyncWriter);

            Assert.Equal(expected, writer.ToString());
            Assert.Equal(expected, asyncWriter.ToString());
            AssertLineBreaks(newLine, expected);
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public async Task SaveToFile_uses_the_new_line(string newLine)
        {
            var report = CreateReport(newLine);
            var expected = report.GetTextReport() + newLine;

            var path = report.SaveToFile(Path.Combine(directory, "report.txt"));
            var asyncPath = await report.SaveToFileAsync(Path.Combine(directory, "report-async.txt"));

            Assert.Equal(expected, File.ReadAllText(path, Encoding.UTF8));
            Assert.Equal(expected, File.ReadAllText(asyncPath, Encoding.UTF8));
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public async Task SavePagesToFiles_uses_the_new_line(string newLine)
        {
            var report = CreateReport(newLine);
            var pages = report.GetPagesReport();

            var paths = report.SavePagesToFiles(Path.Combine(directory, "sync"));
            var asyncPaths = await report.SavePagesToFilesAsync(Path.Combine(directory, "async"));

            for (int i = 0; i < pages.Length; i++)
            {
                var expected = string.Join(newLine, pages[i].Lines) + newLine;
                Assert.Equal(expected, File.ReadAllText(paths[i], Encoding.UTF8));
                Assert.Equal(expected, File.ReadAllText(asyncPaths[i], Encoding.UTF8));
                AssertLineBreaks(newLine, expected);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Without_new_line_uses_the_environment_one(string? newLine)
        {
            var report = CreateReport(newLine, paged: false);

            Assert.Equal(string.Join(Environment.NewLine, report.GetTextLinesReport()), report.GetTextReport());
        }
    }
}

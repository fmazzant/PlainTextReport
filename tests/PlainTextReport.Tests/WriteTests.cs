using System.Text;
using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class WriteTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "PlainTextReportTests", Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static IPlainTextReport<Item> CreateReport(bool paged = true)
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
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(m => m.Name, style => { });
            }).Bind(Enumerable.Range(1, 25).Select(i => new Item { Id = i, Name = $"N{i}…" }));
        }

        private static string Expected(IPlainTextReport<Item> report) => report.GetTextReport() + Environment.NewLine;

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WriteTo_writes_the_report_in_a_text_writer(bool paged)
        {
            var report = CreateReport(paged);
            var writer = new StringWriter();

            report.WriteTo(writer);

            Assert.Equal(Expected(report), writer.ToString());
        }

        [Fact]
        public async Task WriteToAsync_writes_the_report_in_a_text_writer()
        {
            var report = CreateReport();
            var writer = new StringWriter();

            await report.WriteToAsync(writer);

            Assert.Equal(Expected(report), writer.ToString());
            Assert.Equal(2, writer.ToString().Count(c => c == '\f'));
        }

        [Fact]
        public void WriteTo_writes_utf8_without_bom_and_leaves_the_stream_open()
        {
            var report = CreateReport();
            using var stream = new MemoryStream();

            report.WriteTo(stream);

            Assert.True(stream.CanWrite);
            var bytes = stream.ToArray();
            Assert.NotEqual(0xEF, bytes[0]);
            Assert.Equal(Expected(report), Encoding.UTF8.GetString(bytes));
        }

        [Fact]
        public async Task WriteToAsync_uses_the_encoding()
        {
            var report = CreateReport();
            using var stream = new MemoryStream();

            await report.WriteToAsync(stream, Encoding.Unicode);

            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.Unicode);
            Assert.Equal(Expected(report), reader.ReadToEnd());
        }

        [Fact]
        public void WriteTo_appends_to_the_stream()
        {
            var report = CreateReport(paged: false);
            using var stream = new MemoryStream();
            stream.Write(Encoding.UTF8.GetBytes("before\n"));

            report.WriteTo(stream);

            Assert.Equal("before\n" + Expected(report), Encoding.UTF8.GetString(stream.ToArray()));
        }

        [Fact]
        public async Task Write_methods_require_the_target()
        {
            var report = CreateReport();

            Assert.Throws<ArgumentNullException>(() => report.WriteTo((TextWriter)null!));
            Assert.Throws<ArgumentNullException>(() => report.WriteTo((Stream)null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => report.WriteToAsync((TextWriter)null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => report.WriteToAsync((Stream)null!));
        }

        [Fact]
        public async Task WriteToAsync_can_be_canceled()
        {
            var report = CreateReport();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => report.WriteToAsync(new StringWriter(), cancellation.Token));
        }

        [Fact]
        public async Task SaveToFileAsync_writes_the_same_file_as_SaveToFile()
        {
            var report = CreateReport();

            var syncPath = report.SaveToFile(Path.Combine(directory, "sync.txt"));
            var asyncPath = await report.SaveToFileAsync(Path.Combine(directory, "nested", "async.txt"));

            Assert.Equal(Path.Combine(directory, "nested", "async.txt"), asyncPath);
            Assert.Equal(File.ReadAllBytes(syncPath), File.ReadAllBytes(asyncPath));
        }

        [Fact]
        public async Task SaveToFileAsync_validates_the_path()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => CreateReport().SaveToFileAsync(" "));
        }

        [Fact]
        public async Task SavePagesToFilesAsync_writes_a_file_for_every_page()
        {
            var report = CreateReport();
            var pages = report.GetPagesReport();

            var paths = await report.SavePagesToFilesAsync(directory, "p{page}.txt");

            Assert.Equal(new[] { "p1.txt", "p2.txt", "p3.txt" }, paths.Select(Path.GetFileName));
            for (int i = 0; i < pages.Length; i++)
            {
                Assert.Equal(pages[i] + Environment.NewLine, File.ReadAllText(paths[i]));
            }
        }

        [Fact]
        public async Task SavePagesToFilesAsync_can_be_canceled()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateReport().SavePagesToFilesAsync(directory, cancellationToken: cancellation.Token));
            Assert.Empty(Directory.GetFiles(directory));
        }
    }
}

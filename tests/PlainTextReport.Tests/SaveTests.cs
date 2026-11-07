using System.Text;
using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class SaveTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "PlainTextReportTests", Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static IPlainTextReport<Item> CreateReport(string title = "Report", int items = 25)
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
                options.Title = title;
                options.ShowHeader = true;
                options.Page = page;
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(m => m.Name, style => { });
            }).Bind(Enumerable.Range(1, items).Select(i => new Item { Id = i, Name = $"N{i}…" }));
        }

        [Fact]
        public void SaveToFile_writes_the_whole_report()
        {
            var report = CreateReport();
            var path = Path.Combine(directory, "report.txt");
            Directory.CreateDirectory(directory);

            report.SaveToFile(path);

            var bytes = File.ReadAllBytes(path);
            Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "no BOM");
            Assert.Equal(report.GetTextReport() + Environment.NewLine, File.ReadAllText(path, Encoding.UTF8));
            Assert.Equal(2, File.ReadAllText(path).Count(c => c == '\f'));
        }

        [Fact]
        public void SaveToFile_uses_the_encoding()
        {
            var report = CreateReport();
            var path = Path.Combine(directory, "report.txt");
            Directory.CreateDirectory(directory);

            report.SaveToFile(path, Encoding.Unicode);

            Assert.Equal(report.GetTextReport() + Environment.NewLine, File.ReadAllText(path, Encoding.Unicode));
        }

        [Fact]
        public void SavePagesToFiles_writes_a_file_for_every_page()
        {
            var report = CreateReport();
            var pages = report.GetPagesReport();

            var paths = report.SavePagesToFiles(directory);

            Assert.Equal(new[] { "page-001.txt", "page-002.txt", "page-003.txt" }, paths.Select(Path.GetFileName));
            for (int i = 0; i < pages.Length; i++)
            {
                Assert.Equal(pages[i] + Environment.NewLine, File.ReadAllText(paths[i]));
            }
        }

        [Fact]
        public void SavePagesToFiles_uses_the_pattern_and_removes_invalid_characters()
        {
            var paths = CreateReport(title: "Sales/2026").SavePagesToFiles(directory, "{title}-{date:yyyyMMdd}-{page}of{pages}.txt");

            Assert.Equal(new[] { "Sales_2026-20261107-1of3.txt", "Sales_2026-20261107-2of3.txt", "Sales_2026-20261107-3of3.txt" }, paths.Select(Path.GetFileName));
            Assert.All(paths, path => Assert.True(File.Exists(path)));
        }

        [Fact]
        public void SavePagesToFiles_requires_a_different_name_for_every_page()
        {
            Assert.Throws<ArgumentException>(() => CreateReport().SavePagesToFiles(directory, "report.txt"));
            Assert.False(Directory.Exists(directory));
        }

        [Fact]
        public void SavePagesToFiles_without_page_settings_writes_one_file()
        {
            var report = new PlainTextReport<Item>(options => options.Columns.AddColumn(m => m.Id, style => { }))
                .Bind(new[] { new Item { Id = 1 } });

            var paths = report.SavePagesToFiles(directory, "report.txt");

            Assert.Single(paths);
            Assert.Equal("1" + Environment.NewLine, File.ReadAllText(paths[0]));
        }
    }
}

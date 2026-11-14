using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class StreamingTests
    {
        private static PlainTextReportPageSettings SmallPage()
        {
            // 20 characters x 12 lines
            return new PlainTextReportPageSettings(PaperSize.Custom("Test", 25.4 * 2, 25.4 * 3))
            {
                CharactersPerInch = 10,
                LinesPerInch = 4,
                Date = new DateTime(2026, 11, 7),
            };
        }

        private static IEnumerable<Item> Items(int count, Action<int>? onRead = null)
        {
            for (int i = 1; i <= count; i++)
            {
                onRead?.Invoke(i);
                yield return new Item { Id = i, Name = i % 4 == 0 ? $"Name {i} with a long text" : $"N{i}" };
            }
        }

        private static PlainTextReport<Item> CreateReport(Action<PlainTextReportOptions<Item>> configure, bool fixedWidths)
        {
            return new PlainTextReport<Item>(options =>
            {
                options.Title = "Report";
                options.ShowHeader = true;
                options.ShowHeaderSeparator = true;
                options.ShowColumnSeparator = true;
                options.Columns.AddColumn(m => m.Id, style => { if (fixedWidths) style.Width = 3; });
                options.Columns.AddColumn(m => m.Name, style => { if (fixedWidths) style.Width = 12; });
                configure(options);
            });
        }

        public static TheoryData<string, bool> Configurations => new TheoryData<string, bool>
        {
            { "none", true },
            { "none", false },
            { "width", true },
            { "width", false },
            { "page", true },
            { "page", false },
            { "pages", true },
            { "pages", false },
            { "multiline", true },
            { "multiline", false },
            { "multiline-pages", true },
            { "multiline-pages", false },
            { "groups", true },
            { "groups", false },
            { "groups-pages", true },
            { "groups-pages", false },
        };

        private static void Configure(PlainTextReportOptions<Item> options, string configuration)
        {
            switch (configuration)
            {
                case "width":
                    options.Width = 18;
                    break;
                case "page":
                    options.Page = SmallPage();
                    options.Page.Header.Left = "{title}";
                    options.Page.Footer.Center = "- {page} -";
                    break;
                case "pages":
                    options.Page = SmallPage();
                    options.Page.Footer.Right = "{page}/{pages:00}";
                    options.Page.Footer.ShowSeparator = true;
                    break;
                case "multiline":
                    options.Width = 14;
                    options.MultiLineRows = true;
                    break;
                case "multiline-pages":
                    options.Page = SmallPage();
                    options.Page.RepeatColumnHeader = true;
                    options.Page.Header.Right = "Page {page} of {pages}";
                    options.MultiLineRows = true;
                    options.Width = 14;
                    break;
                case "groups":
                    options.ShowTotals = true;
                    options.GroupBy(m => m.Id / 7, group =>
                    {
                        group.Header = "Group {key}";
                        group.ShowSubtotals = true;
                    });
                    break;
                case "groups-pages":
                    options.Page = SmallPage();
                    options.Page.Footer.Right = "{page}/{pages}";
                    options.ShowTotals = true;
                    options.GroupBy(m => m.Id / 7, group =>
                    {
                        group.Header = "Group {key}";
                        group.ShowSubtotals = true;
                        group.NewPage = true;
                    });
                    break;
            }
        }

        [Theory]
        [MemberData(nameof(Configurations))]
        public void WriteTo_writes_the_same_text_as_GetTextReport(string configuration, bool fixedWidths)
        {
            var report = CreateReport(options => Configure(options, configuration), fixedWidths);
            var expected = report.Bind(Items(30)).GetTextReport() + Environment.NewLine;

            var writer = new StringWriter();
            report.Bind(Items(30)).WriteTo(writer);

            Assert.Equal(expected, writer.ToString());
        }

        [Theory]
        [MemberData(nameof(Configurations))]
        public async Task WriteToAsync_writes_the_same_text_as_GetTextReport(string configuration, bool fixedWidths)
        {
            var report = CreateReport(options => Configure(options, configuration), fixedWidths);
            var expected = report.Bind(Items(30)).GetTextReport() + Environment.NewLine;

            var writer = new StringWriter();
            await report.Bind(Items(30)).WriteToAsync(writer);

            Assert.Equal(expected, writer.ToString());
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(25, false)]
        public void WriteTo_writes_the_same_pages_as_GetPagesReport(int count, bool fixedWidths)
        {
            var report = CreateReport(options => Configure(options, "pages"), fixedWidths);
            var pages = report.Bind(Items(count)).GetPagesReport();

            var writer = new StringWriter();
            report.Bind(Items(count)).WriteTo(writer);

            Assert.Equal(string.Join(Environment.NewLine + "\f", pages.Select(p => p.ToString())) + Environment.NewLine, writer.ToString());
        }

        [Fact]
        public void WriteTo_without_columns_writes_the_title()
        {
            var report = new PlainTextReport<Item>(options => options.Title = "Report");
            var writer = new StringWriter();

            report.Bind(Items(3)).WriteTo(writer);

            Assert.Equal(report.GetTextReport() + Environment.NewLine, writer.ToString());
        }

        [Theory]
        [InlineData("none")]
        [InlineData("page")]
        [InlineData("groups")]
        public void WriteTo_with_fixed_widths_reads_the_values_while_writing(string configuration)
        {
            var report = CreateReport(options => Configure(options, configuration), fixedWidths: true);
            var read = 0;
            var writer = new ProbeWriter(() => read);

            report.Bind(Items(100, i => read = i)).WriteTo(writer);

            // the first lines were written when only the rows of the first page had been read
            Assert.True(writer.ReadAtFirstWrite < 12, $"{writer.ReadAtFirstWrite} values read before the first write");
            Assert.Equal(100, read);
        }

        [Theory]
        [InlineData(18)]
        [InlineData(0)]
        public void WriteTo_with_percent_widths_reads_the_values_while_writing_only_with_a_report_width(int width)
        {
            var report = new PlainTextReport<Item>(options =>
            {
                options.Width = width;
                options.Columns.AddColumn(m => m.Id, style => style.Width = 3);
                options.Columns.AddColumn(m => m.Name, style => style.Width = "100%");
            });
            var read = 0;
            var writer = new ProbeWriter(() => read);

            report.Bind(Items(100, i => read = i)).WriteTo(writer);

            // the first row is written after it is read; without a report width a percentage column is automatic
            Assert.Equal(width > 0 ? 1 : 100, writer.ReadAtFirstWrite);
            Assert.Equal(report.GetTextReport() + Environment.NewLine, writer.ToString());
        }

        [Theory]
        [InlineData("none")]
        [InlineData("pages")]
        public void WriteTo_with_automatic_widths_or_page_count_reads_the_values_first(string configuration)
        {
            var fixedWidths = configuration == "pages";
            var report = CreateReport(options => Configure(options, configuration), fixedWidths);
            var read = 0;
            var writer = new ProbeWriter(() => read);

            report.Bind(Items(100, i => read = i)).WriteTo(writer);

            Assert.Equal(100, writer.ReadAtFirstWrite);
        }

        [Theory]
        [InlineData("none", true)]
        [InlineData("none", false)]
        [InlineData("pages", true)]
        [InlineData("multiline-pages", false)]
        public void WriteTo_reads_the_values_once(string configuration, bool fixedWidths)
        {
            var report = CreateReport(options => Configure(options, configuration), fixedWidths);
            var enumerations = 0;
            var values = new CountingEnumerable(Items(30), () => enumerations++);

            report.Bind(values).WriteTo(new StringWriter());

            Assert.Equal(1, enumerations);
        }

        [Fact]
        public void WriteTo_with_fixed_widths_has_written_the_rows_read_before_an_error()
        {
            var report = CreateReport(options => { }, fixedWidths: true);
            var writer = new StringWriter();

            Assert.Throws<InvalidOperationException>(() => report.Bind(Items(10, i => { if (i == 6) throw new InvalidOperationException("broken"); })).WriteTo(writer));

            Assert.Contains("N5", writer.ToString());
        }

        [Fact]
        public void WriteTo_with_automatic_widths_writes_nothing_before_an_error()
        {
            var report = CreateReport(options => { }, fixedWidths: false);
            var writer = new StringWriter();

            Assert.Throws<InvalidOperationException>(() => report.Bind(Items(10, i => { if (i == 6) throw new InvalidOperationException("broken"); })).WriteTo(writer));

            Assert.Equal(string.Empty, writer.ToString());
        }

        /// <summary>
        /// A writer that records how many values had been read at the first write
        /// </summary>
        private sealed class ProbeWriter : StringWriter
        {
            private readonly Func<int> read;

            public ProbeWriter(Func<int> read)
            {
                this.read = read;
            }

            public int? ReadAtFirstWrite { get; private set; }

            public override void Write(string? value)
            {
                ReadAtFirstWrite ??= read();
                base.Write(value);
            }
        }

        /// <summary>
        /// A collection that counts its enumerations
        /// </summary>
        private sealed class CountingEnumerable : IEnumerable<Item>
        {
            private readonly IEnumerable<Item> items;
            private readonly Action onEnumerate;

            public CountingEnumerable(IEnumerable<Item> items, Action onEnumerate)
            {
                this.items = items;
                this.onEnumerate = onEnumerate;
            }

            public IEnumerator<Item> GetEnumerator()
            {
                onEnumerate();
                return items.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}

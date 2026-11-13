using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class PagingTests
    {
        private static Item[] Items(int count) =>
            Enumerable.Range(1, count).Select(i => new Item { Id = i, Name = $"N{i}" }).ToArray();

        // 2 x 3 inches: portrait at 10 cpi / 4 lpi is 20 characters x 12 lines
        private static PlainTextReportPageSettings SmallPage() =>
            new PlainTextReportPageSettings(PaperSize.Custom("Test", 25.4 * 2, 25.4 * 3))
            {
                CharactersPerInch = 10,
                LinesPerInch = 4,
                Date = new DateTime(2026, 11, 7, 9, 30, 0),
            };

        private static PlainTextReport<Item> CreateReport(PlainTextReportPageSettings page, Action<PlainTextReportOptions<Item>>? configure = null)
        {
            return new PlainTextReport<Item>(options =>
            {
                options.Title = "Report";
                options.ShowHeader = true;
                options.ShowHeaderSeparator = true;
                options.ShowColumnSeparator = true;
                options.Page = page;
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(m => m.Name, style => { });
                configure?.Invoke(options);
            });
        }

        [Theory]
        [InlineData(0, 841, 1189)]
        [InlineData(1, 594, 841)]
        [InlineData(2, 420, 594)]
        [InlineData(3, 297, 420)]
        [InlineData(4, 210, 297)]
        [InlineData(5, 148, 210)]
        [InlineData(6, 105, 148)]
        [InlineData(10, 26, 37)]
        public void A_series_has_the_iso_sizes(int number, double shortSide, double longSide)
        {
            var paper = PaperSize.A(number);

            Assert.Equal("A" + number, paper.Name);
            Assert.Equal(shortSide, paper.ShortSideMillimeters);
            Assert.Equal(longSide, paper.LongSideMillimeters);
        }

        [Fact]
        public void A_series_is_limited_to_A10()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PaperSize.A(11));
            Assert.Throws<ArgumentOutOfRangeException>(() => PaperSize.A(-1));
        }

        [Theory]
        [InlineData(0, 1000, 1414)]
        [InlineData(1, 707, 1000)]
        [InlineData(4, 250, 353)]
        [InlineData(5, 176, 250)]
        [InlineData(10, 31, 44)]
        public void B_series_has_the_iso_sizes(int number, double shortSide, double longSide)
        {
            var paper = PaperSize.B(number);

            Assert.Equal("B" + number, paper.Name);
            Assert.Equal(shortSide, paper.ShortSideMillimeters);
            Assert.Equal(longSide, paper.LongSideMillimeters);
        }

        [Fact]
        public void B_series_is_limited_to_B10()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PaperSize.B(11));
            Assert.Throws<ArgumentOutOfRangeException>(() => PaperSize.B(-1));
        }

        public static TheoryData<PaperSize, string, double, double> NamedPapers => new()
        {
            { PaperSize.B4, "B4", 250, 353 },
            { PaperSize.B5, "B5", 176, 250 },
            { PaperSize.Letter, "Letter", 215.9, 279.4 },
            { PaperSize.Legal, "Legal", 215.9, 355.6 },
            { PaperSize.Tabloid, "Tabloid", 279.4, 431.8 },
            { PaperSize.Executive, "Executive", 184.15, 266.7 },
            { PaperSize.ContinuousForm, "ContinuousForm", 240, 304.8 },
        };

        [Theory]
        [MemberData(nameof(NamedPapers))]
        public void Named_papers_have_their_sizes(PaperSize paper, string name, double shortSide, double longSide)
        {
            Assert.Equal(name, paper.Name);
            Assert.Equal(shortSide, paper.ShortSideMillimeters);
            Assert.Equal(longSide, paper.LongSideMillimeters);
        }

        // the values of the table in the README
        [Theory]
        [InlineData("B4", 98, 83, 138, 59)]
        [InlineData("B5", 69, 59, 98, 41)]
        [InlineData("Letter", 85, 66, 110, 51)]
        [InlineData("Legal", 85, 84, 140, 51)]
        [InlineData("Tabloid", 110, 102, 170, 66)]
        [InlineData("Executive", 72, 63, 105, 43)]
        [InlineData("ContinuousForm", 94, 72, 120, 56)]
        public void Named_papers_are_converted_to_characters_and_lines(string name, int portraitWidth, int portraitHeight, int landscapeWidth, int landscapeHeight)
        {
            var paper = (PaperSize)typeof(PaperSize).GetProperty(name)!.GetValue(null)!;
            var portrait = new PlainTextReportPageSettings(paper, PageOrientation.Portrait);
            var landscape = new PlainTextReportPageSettings(paper, PageOrientation.Landscape);

            Assert.Equal(portraitWidth, portrait.Width);
            Assert.Equal(portraitHeight, portrait.Height);
            Assert.Equal(landscapeWidth, landscape.Width);
            Assert.Equal(landscapeHeight, landscape.Height);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Custom_paper_without_a_name_is_named_Custom(string? name)
        {
            var paper = PaperSize.Custom(name!, 50.8, 76.2);

            Assert.Equal("Custom", paper.Name);
            Assert.Equal("Custom (50.8 x 76.2 mm)", paper.ToString());
        }

        [Fact]
        public void Custom_paper_keeps_its_name()
        {
            Assert.Equal("Label", PaperSize.Custom("Label", 50.8, 76.2).Name);
        }

        [Theory]
        [InlineData(4, PageOrientation.Portrait, 82, 70)]
        [InlineData(4, PageOrientation.Landscape, 116, 49)]
        [InlineData(3, PageOrientation.Portrait, 116, 99)]
        [InlineData(3, PageOrientation.Landscape, 165, 70)]
        [InlineData(5, PageOrientation.Portrait, 58, 49)]
        public void Paper_is_converted_to_characters_and_lines(int number, PageOrientation orientation, int width, int height)
        {
            var page = new PlainTextReportPageSettings(PaperSize.A(number), orientation);

            Assert.Equal(width, page.Width);
            Assert.Equal(height, page.Height);
        }

        [Fact]
        public void Splits_the_report_in_pages_with_header_and_footer()
        {
            var page = SmallPage();
            page.Header.Left = "{title}";
            page.Header.Right = "Page {page}/{pages}";
            page.Header.ShowSeparator = true;
            page.Footer.Center = "- {page} -";

            var pages = CreateReport(page).Bind(Items(12)).GetPagesReport();

            // 12 lines - 2 for the header - 1 for the footer = 9 lines: column header (2) + 7 rows
            Assert.Equal(2, pages.Length);
            Assert.All(pages, p => Assert.Equal(12, p.Lines.Length));
            Assert.All(pages, p => Assert.All(p.Lines, line => Assert.True(line.Length <= 20, line)));

            Assert.Equal(new[]
            {
                "Report      Page 1/2",
                "--------------------",
                "Id | Name",
                "---+-----",
                " 1 | N1",
                " 2 | N2",
                " 3 | N3",
                " 4 | N4",
                " 5 | N5",
                " 6 | N6",
                " 7 | N7",
                "       - 1 -",
            }, pages[0].Lines);

            Assert.Equal(new[]
            {
                "Report      Page 2/2",
                "--------------------",
                "Id | Name",
                "---+-----",
                " 8 | N8",
                " 9 | N9",
                "10 | N10",
                "11 | N11",
                "12 | N12",
                "",
                "",
                "       - 2 -",
            }, pages[1].Lines);

            Assert.Equal(1, pages[0].Number);
            Assert.Equal(2, pages[1].Count);
        }

        [Fact]
        public void Title_is_printed_only_on_the_first_page()
        {
            var pages = CreateReport(SmallPage(), o => o.ShowTitle = true).Bind(Items(20)).GetPagesReport();

            Assert.Equal("Report", pages[0].Lines[0].Trim());
            Assert.Equal("Id | Name", pages[0].Lines[2]);
            Assert.Equal("Id | Name", pages[1].Lines[0]);
        }

        [Fact]
        public void Column_header_can_be_printed_only_on_the_first_page()
        {
            var page = SmallPage();
            page.RepeatColumnHeader = false;

            var pages = CreateReport(page).Bind(Items(15)).GetPagesReport();

            Assert.Equal("Id | Name", pages[0].Lines[0]);
            Assert.Equal("11 | N11", pages[1].Lines[0]);
        }

        [Fact]
        public void Applies_the_margins()
        {
            var page = SmallPage();
            page.MarginLeft = 2;
            page.MarginRight = 3;
            page.MarginTop = 1;
            page.MarginBottom = 1;
            page.Footer.Right = "{page}";

            var pages = CreateReport(page).Bind(Items(3)).GetPagesReport();

            var lines = pages[0].Lines;
            Assert.Equal(12, lines.Length);
            Assert.Equal("", lines[0]);
            Assert.Equal("  Id | Name", lines[1]);
            Assert.Equal("                1", lines[10]);
            Assert.Equal("", lines[11]);
            Assert.All(lines, line => Assert.True(line.Length <= 20 - 3, line));
        }

        [Fact]
        public void Replaces_the_placeholders_with_format()
        {
            var page = SmallPage();
            page.Header.Left = "{date:dd/MM/yyyy}";
            page.Header.Right = "{page:000}";
            page.Footer.Left = "{date}\n{pages} pages";

            var lines = CreateReport(page).Bind(Items(1)).GetPagesReport()[0].Lines;

            Assert.Equal("07/11/2026       001", lines[0]);
            Assert.Equal("2026-11-07", lines[10]);
            Assert.Equal("1 pages", lines[11]);
        }

        [Fact]
        public void The_table_fits_the_printable_width()
        {
            var page = new PlainTextReportPageSettings(PaperSize.A10, PageOrientation.Portrait);
            var items = new[] { new Item { Id = 1, Name = "A very long name" } };

            var lines = CreateReport(page).Bind(items).GetTextLinesReport();

            Assert.Equal(10, page.PrintableWidth);
            Assert.All(lines, line => Assert.True(line.Length <= 10, line));
            Assert.Contains(" 1 | A ve…", lines);
        }

        [Fact]
        public void GetTextReport_separates_the_pages_with_the_page_break()
        {
            var pages = CreateReport(SmallPage()).Bind(Items(25)).GetPagesReport();
            var text = CreateReport(SmallPage()).Bind(Items(25)).GetTextReport();

            Assert.Equal(3, pages.Length);
            Assert.Equal(2, text.Count(c => c == '\f'));
            Assert.Equal(string.Join(Environment.NewLine + "\f", pages.Select(p => p.ToString())), text);

            var noBreak = SmallPage();
            noBreak.PageBreak = "";
            Assert.DoesNotContain('\f', CreateReport(noBreak).Bind(Items(25)).GetTextReport());
        }

        [Fact]
        public void GetTextLinesReport_returns_full_pages()
        {
            var lines = CreateReport(SmallPage()).Bind(Items(25)).GetTextLinesReport();

            Assert.Equal(3 * 12, lines.Length);
        }

        [Fact]
        public void Empty_report_is_one_page()
        {
            var pages = CreateReport(SmallPage()).Bind(Array.Empty<Item>()).GetPagesReport();

            Assert.Single(pages);
            Assert.Equal(12, pages[0].Lines.Length);
        }

        [Fact]
        public void Without_page_settings_the_report_is_one_page()
        {
            var pages = CreateReport(null!).Bind(Items(100)).GetPagesReport();

            Assert.Single(pages);
            Assert.Equal(102, pages[0].Lines.Length);
        }

        [Fact]
        public void Throws_when_the_page_is_too_small()
        {
            var page = SmallPage();
            page.Header.Left = string.Join("\n", Enumerable.Repeat("x", 12));

            Assert.Throws<InvalidOperationException>(() => CreateReport(page).Bind(Items(1)).GetPagesReport());
        }
    }
}

using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class UnicodeWidthTests
    {
        // the texts are written with escapes, so the test does not depend on the font of the editor
        private const string Nihon = "日本";                       // 日本, 2 wide characters
        private const string Nihongo = "日本語";               // 日本語
        private const string Tekisuto = "テキスト";        // テキスト
        private const string Grinning = "\U0001F600";                     // 😀, a surrogate pair
        private const string Heart = "❤️";                      // ❤️, with the emoji variation selector
        private const string CafeCombining = "Café";                // Café with a combining acute accent

        [Theory]
        [InlineData("abc", 3)]
        [InlineData("é", 1)]                 // é precomposed
        [InlineData(CafeCombining, 4)]
        [InlineData(Nihon, 4)]
        [InlineData(Nihongo, 6)]
        [InlineData("Ａ", 2)]                 // fullwidth A
        [InlineData(Grinning, 2)]
        [InlineData(Heart, 2)]
        [InlineData("a​b", 2)]               // zero width space
        [InlineData("", 0)]
        public void Measures_the_display_width(string text, int width)
        {
            Assert.Equal(width, TextWidth.Unicode.GetWidth(text));
        }

        [Theory]
        [InlineData(Nihon, 2)]
        [InlineData(Grinning, 2)]
        [InlineData(CafeCombining, 5)]
        public void The_simple_mode_counts_the_code_units(string text, int width)
        {
            Assert.Equal(width, TextWidth.Simple.GetWidth(text));
        }

        [Theory]
        [InlineData(Nihongo, 5, 2)]
        [InlineData(Nihongo, 6, 3)]
        [InlineData("a" + Grinning + "b", 2, 1)]
        [InlineData("a" + Grinning + "b", 3, 3)]
        [InlineData(CafeCombining, 4, 5)]
        [InlineData(CafeCombining, 3, 3)]
        public void Fits_whole_text_elements(string text, int width, int length)
        {
            Assert.Equal(length, TextWidth.Unicode.GetFitLength(text, width));
        }

        private static string[] Render(IEnumerable<Product> products, Action<PlainTextReportOptions<Product>>? configure = null)
        {
            return new PlainTextReport<Product>(options =>
            {
                options.ShowHeader = true;
                options.ShowColumnSeparator = true;
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(m => m.Name, style => { });
                options.Columns.AddColumn(m => m.Price, style => { });
                configure?.Invoke(options);
            }).Bind(products).GetTextLinesReport();
        }

        private static void AssertWidths(int width, IEnumerable<string> lines)
        {
            Assert.All(lines, line => Assert.Equal(width, TextWidth.Unicode.GetWidth(line)));
        }

        private static void AssertNoBrokenSurrogates(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsHighSurrogate(text[i]))
                {
                    Assert.True(i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]), "broken surrogate pair");
                    i++;
                }
                else
                {
                    Assert.False(char.IsLowSurrogate(text[i]), "broken surrogate pair");
                }
            }
        }

        [Fact]
        public void Aligns_the_wide_characters()
        {
            var lines = Render(new[]
            {
                new Product { Id = 1, Name = Nihon, Price = 1.5m },
                new Product { Id = 2, Name = "Apple", Price = 2m },
                new Product { Id = 3, Name = Grinning + Grinning, Price = 3m },
                new Product { Id = 4, Name = CafeCombining, Price = 4m },
            });

            Assert.Equal(new[]
            {
                "Id | Name  | Price",
                " 1 | " + Nihon + "  |   1.5",
                " 2 | Apple |     2",
                " 3 | " + Grinning + Grinning + "  |     3",
                " 4 | " + CafeCombining + "  |     4",
            }, lines);
            AssertWidths(18, lines);
        }

        [Fact]
        public void The_simple_mode_keeps_the_old_behavior()
        {
            var lines = Render(new[] { new Product { Id = 1, Name = Nihongo, Price = 1.5m } }, o => o.UnicodeWidth = false);

            Assert.Equal(" 1 | " + Nihongo + "  |   1.5", lines[1]);
        }

        [Fact]
        public void UnicodeWidth_is_on_by_default()
        {
            Assert.True(new PlainTextReportOptions<Product>().UnicodeWidth);
        }

        [Theory]
        [InlineData(5, Nihon + "…")]
        [InlineData(6, Nihon + "… ")]
        public void Truncates_between_the_wide_characters(int width, string expected)
        {
            var lines = Render(new[] { new Product { Id = 1, Name = Nihongo + Tekisuto, Price = 1.5m } },
                o => o.Columns.Single(c => c.ColumnName == "Name").Style.Width = width);

            Assert.Equal(" 1 | " + expected + " |   1.5", lines[1]);
            AssertWidths(TextWidth.Unicode.GetWidth(lines[0]), lines.Skip(1));
        }

        [Fact]
        public void Never_cuts_a_surrogate_pair_or_a_combining_mark()
        {
            var lines = Render(new[]
            {
                new Product { Id = 1, Name = "a" + Grinning + "b", Price = 1m },
                new Product { Id = 2, Name = "xyz" + CafeCombining, Price = 2m },
            }, o =>
            {
                o.TruncationMarker = "";
                o.Columns.Single(c => c.ColumnName == "Name").Style.Width = 2;
            });

            Assert.Equal(" 1 | a  |     1", lines[1]);
            Assert.Equal(" 2 | xy |     2", lines[2]);
            Assert.All(lines, AssertNoBrokenSurrogates);
        }

        [Fact]
        public void Never_leaves_half_a_surrogate_pair_in_the_title()
        {
            var lines = new PlainTextReport<Product>(options =>
            {
                options.ShowTitle = true;
                options.Title = "ab" + Grinning + Grinning;
                options.Width = 4;
                options.TruncationMarker = "";
                options.Columns.AddColumn(m => m.Id, style => { });
            }).Bind(new[] { new Product { Id = 1 } }).GetTextLinesReport();

            Assert.Equal("ab" + Grinning, lines[0]);
        }

        [Fact]
        public void Wraps_the_wide_characters()
        {
            var lines = PlainTextReportRenderer<Product>.Wrap(Nihongo + " " + Tekisuto, 6, TextWidth.Unicode);

            Assert.Equal(new[] { Nihongo, "テキス", "ト" }, lines);
        }

        [Fact]
        public void A_wide_character_in_a_narrow_column_is_not_lost_when_wrapping()
        {
            var lines = PlainTextReportRenderer<Product>.Wrap(Nihon, 1, TextWidth.Unicode);

            Assert.Equal(new[] { "日", "本" }, lines);
        }

        [Fact]
        public void Header_and_footer_use_the_display_width()
        {
            // 20 characters x 12 lines
            var page = new PlainTextReportPageSettings(PaperSize.Custom("Test", 25.4 * 2, 25.4 * 3))
            {
                CharactersPerInch = 10,
                LinesPerInch = 4,
            };
            page.Header.Left = Nihongo;
            page.Header.Right = "Page {page}";
            page.Footer.Left = Nihongo + Nihongo + Nihongo + "日";   // 20 columns
            page.Footer.Right = "ab";                                      // overwrites the last wide character

            var lines = new PlainTextReport<Product>(options =>
            {
                options.Page = page;
                options.Columns.AddColumn(m => m.Id, style => { });
            }).Bind(new[] { new Product { Id = 1 } }).GetPagesReport()[0].Lines;

            Assert.Equal(Nihongo + new string(' ', 8) + "Page 1", lines[0]);
            Assert.Equal(Nihongo + Nihongo + Nihongo + "ab", lines[lines.Length - 1]);
            AssertWidths(20, new[] { lines[0], lines[lines.Length - 1] });
        }
    }
}

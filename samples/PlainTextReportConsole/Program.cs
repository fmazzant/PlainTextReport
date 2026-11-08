using Mafesoft.PlainTextReport;

var report = new PlainTextReport<Model>(options =>
{
    // Options
    options.ShowTitle = true;
    options.Title = "My first plain text report";
    options.ShowHeader = true;
    options.ShowHeaderSeparator = true;
    options.ShowColumnSeparator = true;
    options.Width = 80;

    // Columns
    options.Columns.AddColumn(m => m.Id, style => { });
    options.Columns.AddColumn(m => m.Name, style => style.Width = 20);
    options.Columns.AddColumn(m => m.Price, style => { }, columnFormat: "N2");
    options.Columns.AddColumn(m => m.CreatedOn, style => { }, columnFormat: "dd/MM/yyyy");
    options.Columns.AddColumn(m => m.Enabled, style => style.Alignment = PlainTextReportAlignment.Center, new CustomBooleanConverter());
});

var models = Enumerable.Range(1, 20)
    .Select(x => new Model
    {
        Id = x,
        Name = $"Product {x}",
        Price = x * 1.4m,
        CreatedOn = DateTime.Today.AddDays(-x),
        Enabled = x % 3 == 0
    })
    .ToList();

var text = report
    .Bind(models)
    .GetTextReport();

Console.WriteLine(text);

// Paged report: A4 portrait, with header and footer on every page
var pagedReport = new PlainTextReport<Model>(options =>
{
    options.Title = "Products";
    options.ShowHeader = true;
    options.ShowHeaderSeparator = true;
    options.ShowColumnSeparator = true;

    options.Page = new PlainTextReportPageSettings(PaperSize.A4, PageOrientation.Landscape)
    {
        MarginLeft = 2,
        MarginRight = 2,
        MarginTop = 1,
        MarginBottom = 1,
    };
    options.Page.Header.Left = "{title}";
    options.Page.Header.Right = "{date:dd/MM/yyyy}";
    options.Page.Header.ShowSeparator = true;
    options.Page.Footer.Right = "{page}/{pages}";
    options.Page.Footer.ShowSeparator = true;

    options.Columns.AddColumn(m => m.Id, style => { });
    options.Columns.AddColumn(m => m.Name, style => style.Width = "100%"); // takes all the free space
    options.Columns.AddColumn(m => m.Price, style => { }, columnFormat: "N2");
    options.Columns.AddColumn(m => m.CreatedOn, style => { }, columnFormat: "dd/MM/yyyy");
});

var pages = pagedReport
    .Bind(Enumerable.Range(1, 150).Select(x => new Model { Id = x, Name = $"Product {x}", Price = x * 1.4m, CreatedOn = DateTime.Today }))
    .GetPagesReport();

foreach (var page in pages)
{
    Console.WriteLine(page);
    Console.WriteLine();
}

// Multi-line rows: long values wrap in their column, line breaks start a new line
var multiLineReport = new PlainTextReport<Model>(options =>
{
    options.ShowHeader = true;
    options.ShowHeaderSeparator = true;
    options.ShowColumnSeparator = true;
    options.MultiLineRows = true;

    options.Columns.AddColumn(m => m.Id, style => { });
    options.Columns.AddColumn(m => m.Name, style => style.Width = 16);
    options.Columns.AddColumn(m => m.Price, style => { }, columnFormat: "N2");
});

Console.WriteLine(multiLineReport
    .Bind(new[]
    {
        new Model { Id = 1, Name = "Red apples from the Italian Alps", Price = 2.5m },
        new Model { Id = 2, Name = "Bananas\nimported", Price = 1.2m },
    })
    .GetTextReport());
Console.WriteLine();

// Save the report in one file (pages separated by form feed) and every page in its own file.
// The paths can be absolute or relative; the directories are created when missing.
var outputDirectory = Path.Combine(Path.GetTempPath(), "PlainTextReportSample");
var reportFile = pagedReport.SaveToFile(Path.Combine(outputDirectory, "report.txt"));
var files = pagedReport.SavePagesToFiles(Path.Combine(outputDirectory, "pages"), "page-{page:000}.txt");
Console.WriteLine($"Saved {reportFile} and {files.Length} pages in {Path.GetDirectoryName(files[0])}");

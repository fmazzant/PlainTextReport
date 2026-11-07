using Mafesoft.PlainTextReport;
using Mafesoft.PlainTextReport.Conversions;

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

    options.Page = new PlainTextReportPageSettings(PaperSize.A4, PageOrientation.Portrait)
    {
        MarginLeft = 4,
        MarginRight = 4,
        MarginTop = 2,
        MarginBottom = 2,
    };
    options.Page.Header.Left = "{title}";
    options.Page.Header.Right = "{date:dd/MM/yyyy}";
    options.Page.Header.ShowSeparator = true;
    options.Page.Footer.Center = "Page {page} of {pages}";
    options.Page.Footer.ShowSeparator = true;

    options.Columns.AddColumn(m => m.Id, style => { });
    options.Columns.AddColumn(m => m.Name, style => { });
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

public class Model
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool Enabled { get; set; }
}

public class CustomBooleanConverter : IValueConverter
{
    public string Convert(object value, object parameter, IFormatProvider provider)
        => value is bool boolValue && boolValue ? "Yes" : "No";
}

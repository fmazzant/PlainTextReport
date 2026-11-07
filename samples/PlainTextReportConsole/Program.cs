// See https://aka.ms/new-console-template for more information
using PlainTextReport;
using PlainTextReport.Conversions;

Console.WriteLine("Hello, World!");

var title = "My first plain text report";
var report = new PlainTextReport<Model>(options =>
{
    // Options
    options.ShowTitle = true;
    options.Title = title;
    options.MultiLineTitle = new string[] { };
    options.ShowHeader = true;
    options.ShowHeaderSeparator = true;
    options.ShowColumnSeparator = true;
    options.Width = 80;

    // Columns
    options.Columns.AddColumn(m => m.Id, style =>
    {

    });
    options.Columns.AddColumn(m => m.Name, style =>
    {

    });
    options.Columns.AddColumn(m => m.Price, style =>
    {

    });
    options.Columns.AddColumn(m => m.CreatedOn, style =>
    {

    }, columnFormat: "dd/MM/yyyy");
    options.Columns.AddColumn(m => m.Enabled, style =>
    {

    }, new CustomBooleanConverter());
});

var models = new List<Model>();
Enumerable.Range(1, 100).ToList().ForEach(x =>
{
    models.Add(new Model
    {
        Id = x,
        Name = $"{x}",
        Price = (decimal)(x * (1.4)),
        CreatedOn = DateTime.Now
    });
});


var text = report
    .Bind(models)
    .GetTextReport();
Console.WriteLine(text);
Console.ReadLine();

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
    {
        if (value is bool boolValue && boolValue)
        {
            return "Yes";
        }
        return "No";
    }

    public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider) => false;
}
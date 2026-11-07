namespace Mafesoft.PlainTextReport.Tests
{
    public class Order
    {
        public int Id { get; set; }
        public Customer? Customer { get; set; }
        public DateTime? ShippedOn { get; set; }
        public string Code = string.Empty;
    }
}

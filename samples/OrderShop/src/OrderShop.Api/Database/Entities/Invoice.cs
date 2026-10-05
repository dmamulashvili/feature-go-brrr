namespace OrderShop.Api.Database.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string Currency { get; set; } = "";
    public decimal ExchangeRate { get; set; }
    public decimal Amount { get; set; }
}

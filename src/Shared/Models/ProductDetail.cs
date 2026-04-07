namespace Shared.Models;

public class ProductDetail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Design { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
}

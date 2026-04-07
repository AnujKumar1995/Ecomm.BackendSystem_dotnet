namespace Shared.Models;

public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Guid ProductDetailId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
}

public class Cart
{
    public Guid UserId { get; set; }
    public List<CartItem> Items { get; set; } = new();
    public decimal TotalAmount => Items.Sum(i => i.Price * i.Quantity);
}

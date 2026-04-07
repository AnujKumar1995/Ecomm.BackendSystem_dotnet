using System.Collections.Concurrent;
using Shared.Models;

namespace ProductDetailService.Data;

public class ProductDetailRepository
{
    private readonly ConcurrentDictionary<Guid, ProductDetail> _details = new();

    public ProductDetailRepository()
    {
        // Seed data (you'd get productIds from ProductService in real scenario)
        var d1 = new ProductDetail { ProductId = Guid.Empty, Size = "M", Price = 29.99m, Design = "Crew Neck", Color = "Blue", StockQuantity = 100 };
        var d2 = new ProductDetail { ProductId = Guid.Empty, Size = "L", Price = 31.99m, Design = "Crew Neck", Color = "Red", StockQuantity = 50 };
        _details[d1.Id] = d1;
        _details[d2.Id] = d2;
    }

    public IEnumerable<ProductDetail> GetByProductId(Guid productId)
        => _details.Values.Where(d => d.ProductId == productId).ToList();

    public ProductDetail? GetById(Guid id)
        => _details.GetValueOrDefault(id);

    public ProductDetail Add(ProductDetail detail)
    {
        _details[detail.Id] = detail;
        return detail;
    }

    public bool Remove(Guid id)
        => _details.TryRemove(id, out _);

    public IEnumerable<ProductDetail> GetAll()
        => _details.Values.ToList();
}

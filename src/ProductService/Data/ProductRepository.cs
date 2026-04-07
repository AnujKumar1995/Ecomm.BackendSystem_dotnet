using System.Collections.Concurrent;
using Shared.Models;

namespace ProductService.Data;

public class ProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    public ProductRepository()
    {
        // Seed data
        var p1 = new Product { Name = "Classic T-Shirt", Category = "Clothing", Description = "Premium cotton t-shirt" };
        var p2 = new Product { Name = "Running Shoes", Category = "Footwear", Description = "Lightweight running shoes" };
        var p3 = new Product { Name = "Laptop Backpack", Category = "Accessories", Description = "Water-resistant laptop backpack" };
        _products[p1.Id] = p1;
        _products[p2.Id] = p2;
        _products[p3.Id] = p3;
    }

    public IEnumerable<Product> GetAll() => _products.Values.Where(p => p.IsActive).ToList();

    public Product? GetById(Guid id) => _products.GetValueOrDefault(id);

    public Product Add(Product product)
    {
        _products[product.Id] = product;
        return product;
    }

    public bool Remove(Guid id)
    {
        if (_products.TryGetValue(id, out var product))
        {
            product.IsActive = false;
            return true;
        }
        return false;
    }

    public IEnumerable<Product> GetPaged(int page, int pageSize)
    {
        return _products.Values
            .Where(p => p.IsActive)
            .OrderBy(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public int GetTotalCount() => _products.Values.Count(p => p.IsActive);
}

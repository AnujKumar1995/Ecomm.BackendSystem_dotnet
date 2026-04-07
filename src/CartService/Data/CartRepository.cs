using System.Collections.Concurrent;
using Shared.Models;

namespace CartService.Data;

public class CartRepository
{
    private readonly ConcurrentDictionary<Guid, Cart> _carts = new();

    public Cart GetOrCreate(Guid userId)
    {
        return _carts.GetOrAdd(userId, id => new Cart { UserId = id });
    }

    public Cart? Get(Guid userId)
    {
        return _carts.GetValueOrDefault(userId);
    }

    public void AddItem(Guid userId, CartItem item)
    {
        var cart = GetOrCreate(userId);
        var existingItem = cart.Items.FirstOrDefault(i =>
            i.ProductId == item.ProductId && i.ProductDetailId == item.ProductDetailId);

        if (existingItem != null)
        {
            existingItem.Quantity += item.Quantity;
        }
        else
        {
            cart.Items.Add(item);
        }
    }

    public bool RemoveItem(Guid userId, Guid itemId)
    {
        var cart = Get(userId);
        if (cart == null) return false;

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return false;

        return cart.Items.Remove(item);
    }

    public void ClearCart(Guid userId)
    {
        if (_carts.TryGetValue(userId, out var cart))
        {
            cart.Items.Clear();
        }
    }
}

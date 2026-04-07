using System.Collections.Concurrent;
using Shared.Models;

namespace OrderOrchestratorService.Data;

public class OrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public Order Add(Order order)
    {
        _orders[order.Id] = order;
        return order;
    }

    public Order? GetById(Guid id) => _orders.GetValueOrDefault(id);

    public IEnumerable<Order> GetByUserId(Guid userId) =>
        _orders.Values.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToList();

    public IEnumerable<Order> GetAll() => _orders.Values.OrderByDescending(o => o.CreatedAt).ToList();
}

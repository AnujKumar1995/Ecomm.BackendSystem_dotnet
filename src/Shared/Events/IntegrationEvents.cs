namespace Shared.Events;

public class IntegrationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;
}

public class OrderCreatedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }

    public OrderCreatedEvent()
    {
        EventType = nameof(OrderCreatedEvent);
    }
}

public class OrderFailedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;

    public OrderFailedEvent()
    {
        EventType = nameof(OrderFailedEvent);
    }
}

public class ProductAddedEvent : IntegrationEvent
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    public ProductAddedEvent()
    {
        EventType = nameof(ProductAddedEvent);
    }
}

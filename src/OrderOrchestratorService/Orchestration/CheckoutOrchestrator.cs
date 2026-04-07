using System.Text.Json;
using OrderOrchestratorService.Data;
using Shared.Events;
using Shared.Messaging;
using Shared.Models;

namespace OrderOrchestratorService.Orchestration;

public class CheckoutOrchestrator
{
    private readonly OrderRepository _orderRepository;
    private readonly HttpClient _cartHttpClient;
    private readonly HttpClient _productDetailHttpClient;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<CheckoutOrchestrator> _logger;

    public CheckoutOrchestrator(
        OrderRepository orderRepository,
        IHttpClientFactory httpClientFactory,
        IMessagePublisher messagePublisher,
        ILogger<CheckoutOrchestrator> logger)
    {
        _orderRepository = orderRepository;
        _cartHttpClient = httpClientFactory.CreateClient("CartService");
        _productDetailHttpClient = httpClientFactory.CreateClient("ProductDetailService");
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task<Order> ExecuteCheckout(Guid userId)
    {
        _logger.LogInformation("=== CHECKOUT ORCHESTRATION STARTED for User {UserId} ===", userId);

        // Step 1: Get cart
        _logger.LogInformation("Step 1: Fetching cart for user {UserId}", userId);
        var cart = await GetCart(userId);
        if (cart == null || cart.Items.Count == 0)
        {
            _logger.LogWarning("Checkout failed: Cart is empty for user {UserId}", userId);
            throw new ArgumentException("Cart is empty. Cannot proceed with checkout.");
        }
        _logger.LogInformation("Cart has {ItemCount} items, total: {Total}", cart.Items.Count, cart.TotalAmount);

        // Step 2: Validate product details exist (sync call to ProductDetailService)
        _logger.LogInformation("Step 2: Validating product details");
        foreach (var item in cart.Items)
        {
            var detailExists = await ValidateProductDetail(item.ProductDetailId);
            if (!detailExists)
            {
                _logger.LogWarning("Product detail {DetailId} not found. Checkout aborted.", item.ProductDetailId);
                await PublishOrderFailed(userId, Guid.Empty, $"Product detail {item.ProductDetailId} no longer available");
                throw new KeyNotFoundException($"Product detail {item.ProductDetailId} is no longer available.");
            }
        }
        _logger.LogInformation("All product details validated successfully");

        // Step 3: Create order
        _logger.LogInformation("Step 3: Creating order");
        var order = new Order
        {
            UserId = userId,
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductDetailId = i.ProductDetailId,
                ProductName = i.ProductName,
                Size = i.Size,
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList(),
            TotalAmount = cart.TotalAmount,
            Status = OrderStatus.Confirmed
        };

        _orderRepository.Add(order);
        _logger.LogInformation("Order {OrderId} created with status {Status}", order.Id, order.Status);

        // Step 4: Clear cart (sync)
        _logger.LogInformation("Step 4: Clearing cart");
        await ClearCart(userId);

        // Step 5: Publish notification event (async via RabbitMQ)
        _logger.LogInformation("Step 5: Publishing order created event");
        await PublishOrderCreated(order);

        _logger.LogInformation("=== CHECKOUT ORCHESTRATION COMPLETED for Order {OrderId} ===", order.Id);
        return order;
    }

    private async Task<Cart?> GetCart(Guid userId)
    {
        try
        {
            var response = await _cartHttpClient.GetAsync($"/api/cart/{userId}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<Cart>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch cart for user {UserId}", userId);
            return null;
        }
    }

    private async Task<bool> ValidateProductDetail(Guid detailId)
    {
        try
        {
            var response = await _productDetailHttpClient.GetAsync($"/api/productdetails/{detailId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate product detail {DetailId}", detailId);
            return false;
        }
    }

    private async Task ClearCart(Guid userId)
    {
        try
        {
            await _cartHttpClient.DeleteAsync($"/api/cart/{userId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear cart for user {UserId}. Cart will be stale.", userId);
        }
    }

    private async Task PublishOrderCreated(Order order)
    {
        var @event = new OrderCreatedEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            TotalAmount = order.TotalAmount,
            ItemCount = order.Items.Count
        };

        try
        {
            await _messagePublisher.PublishAsync("ecommerce.events", "order.created", @event);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish OrderCreatedEvent. Notification will be missed.");
        }
    }

    private async Task PublishOrderFailed(Guid userId, Guid orderId, string reason)
    {
        var @event = new OrderFailedEvent
        {
            OrderId = orderId,
            UserId = userId,
            Reason = reason
        };

        try
        {
            await _messagePublisher.PublishAsync("ecommerce.events", "order.failed", @event);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish OrderFailedEvent.");
        }
    }
}

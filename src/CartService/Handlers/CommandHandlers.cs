using CartService.Commands;
using CartService.Data;
using MediatR;
using Shared.Models;

namespace CartService.Handlers;

public class AddToCartHandler : IRequestHandler<AddToCartCommand, Cart>
{
    private readonly CartRepository _repository;
    private readonly ILogger<AddToCartHandler> _logger;

    public AddToCartHandler(CartRepository repository, ILogger<AddToCartHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<Cart> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var item = new CartItem
        {
            ProductId = request.ProductId,
            ProductDetailId = request.ProductDetailId,
            ProductName = request.ProductName,
            Size = request.Size,
            Price = request.Price,
            Quantity = request.Quantity
        };

        _repository.AddItem(request.UserId, item);
        var cart = _repository.GetOrCreate(request.UserId);
        _logger.LogInformation("Item added to cart for user {UserId}: {ProductName}", request.UserId, request.ProductName);

        return Task.FromResult(cart);
    }
}

public class RemoveFromCartHandler : IRequestHandler<RemoveFromCartCommand, bool>
{
    private readonly CartRepository _repository;
    private readonly ILogger<RemoveFromCartHandler> _logger;

    public RemoveFromCartHandler(CartRepository repository, ILogger<RemoveFromCartHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<bool> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        var result = _repository.RemoveItem(request.UserId, request.ItemId);
        _logger.LogInformation("Cart item removal for user {UserId}: {Result}", request.UserId, result);
        return Task.FromResult(result);
    }
}

public class ClearCartHandler : IRequestHandler<ClearCartCommand, bool>
{
    private readonly CartRepository _repository;
    private readonly ILogger<ClearCartHandler> _logger;

    public ClearCartHandler(CartRepository repository, ILogger<ClearCartHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<bool> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        _repository.ClearCart(request.UserId);
        _logger.LogInformation("Cart cleared for user {UserId}", request.UserId);
        return Task.FromResult(true);
    }
}

using MediatR;
using Shared.Models;

namespace CartService.Commands;

public record AddToCartCommand(Guid UserId, Guid ProductId, Guid ProductDetailId, string ProductName, string Size, decimal Price, int Quantity) : IRequest<Cart>;

public record RemoveFromCartCommand(Guid UserId, Guid ItemId) : IRequest<bool>;

public record ClearCartCommand(Guid UserId) : IRequest<bool>;

using MediatR;
using Shared.Models;

namespace ProductDetailService.Commands;

public record AddProductDetailCommand(Guid ProductId, string Size, decimal Price, string Design, string Color, int StockQuantity) : IRequest<ProductDetail>;

public record RemoveProductDetailCommand(Guid Id) : IRequest<bool>;

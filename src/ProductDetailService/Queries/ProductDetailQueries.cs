using MediatR;
using Shared.Models;

namespace ProductDetailService.Queries;

public record GetProductDetailsQuery(Guid ProductId) : IRequest<List<ProductDetail>>;

public record GetProductDetailByIdQuery(Guid Id) : IRequest<ProductDetail?>;

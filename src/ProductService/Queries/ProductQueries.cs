using MediatR;
using Shared.Models;

namespace ProductService.Queries;

public record GetAllProductsQuery(int Page = 1, int PageSize = 10) : IRequest<PagedResult<Product>>;

public record GetProductByIdQuery(Guid Id) : IRequest<Product?>;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

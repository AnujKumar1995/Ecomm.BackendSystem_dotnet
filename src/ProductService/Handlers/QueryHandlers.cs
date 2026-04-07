using MediatR;
using ProductService.Data;
using ProductService.Queries;
using Shared.Models;

namespace ProductService.Handlers;

public class GetAllProductsHandler : IRequestHandler<GetAllProductsQuery, PagedResult<Product>>
{
    private readonly ProductRepository _repository;

    public GetAllProductsHandler(ProductRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<Product>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var items = _repository.GetPaged(request.Page, request.PageSize).ToList();
        var totalCount = _repository.GetTotalCount();

        return Task.FromResult(new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Product?>
{
    private readonly ProductRepository _repository;

    public GetProductByIdHandler(ProductRepository repository)
    {
        _repository = repository;
    }

    public Task<Product?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_repository.GetById(request.Id));
    }
}

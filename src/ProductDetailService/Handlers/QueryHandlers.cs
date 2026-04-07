using MediatR;
using ProductDetailService.Data;
using ProductDetailService.Queries;
using Shared.Models;

namespace ProductDetailService.Handlers;

public class GetProductDetailsHandler : IRequestHandler<GetProductDetailsQuery, List<ProductDetail>>
{
    private readonly ProductDetailRepository _repository;

    public GetProductDetailsHandler(ProductDetailRepository repository)
    {
        _repository = repository;
    }

    public Task<List<ProductDetail>> Handle(GetProductDetailsQuery request, CancellationToken cancellationToken)
    {
        var details = _repository.GetByProductId(request.ProductId).ToList();
        return Task.FromResult(details);
    }
}

public class GetProductDetailByIdHandler : IRequestHandler<GetProductDetailByIdQuery, ProductDetail?>
{
    private readonly ProductDetailRepository _repository;

    public GetProductDetailByIdHandler(ProductDetailRepository repository)
    {
        _repository = repository;
    }

    public Task<ProductDetail?> Handle(GetProductDetailByIdQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_repository.GetById(request.Id));
    }
}

using MediatR;
using ProductDetailService.Commands;
using ProductDetailService.Data;
using Shared.Models;

namespace ProductDetailService.Handlers;

public class AddProductDetailHandler : IRequestHandler<AddProductDetailCommand, ProductDetail>
{
    private readonly ProductDetailRepository _repository;
    private readonly ILogger<AddProductDetailHandler> _logger;

    public AddProductDetailHandler(ProductDetailRepository repository, ILogger<AddProductDetailHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<ProductDetail> Handle(AddProductDetailCommand request, CancellationToken cancellationToken)
    {
        var detail = new ProductDetail
        {
            ProductId = request.ProductId,
            Size = request.Size,
            Price = request.Price,
            Design = request.Design,
            Color = request.Color,
            StockQuantity = request.StockQuantity
        };

        _repository.Add(detail);
        _logger.LogInformation("Product detail added: {DetailId} for Product {ProductId}", detail.Id, detail.ProductId);

        return Task.FromResult(detail);
    }
}

public class RemoveProductDetailHandler : IRequestHandler<RemoveProductDetailCommand, bool>
{
    private readonly ProductDetailRepository _repository;
    private readonly ILogger<RemoveProductDetailHandler> _logger;

    public RemoveProductDetailHandler(ProductDetailRepository repository, ILogger<RemoveProductDetailHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<bool> Handle(RemoveProductDetailCommand request, CancellationToken cancellationToken)
    {
        var result = _repository.Remove(request.Id);
        _logger.LogInformation("Product detail removal {Result}: {DetailId}", result ? "succeeded" : "failed", request.Id);
        return Task.FromResult(result);
    }
}

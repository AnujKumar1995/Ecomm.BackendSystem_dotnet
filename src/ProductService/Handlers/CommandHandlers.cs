using MediatR;
using ProductService.Commands;
using ProductService.Data;
using Shared.Models;

namespace ProductService.Handlers;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Product>
{
    private readonly ProductRepository _repository;
    private readonly ILogger<CreateProductHandler> _logger;

    public CreateProductHandler(ProductRepository repository, ILogger<CreateProductHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<Product> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Category = request.Category,
            Description = request.Description
        };

        _repository.Add(product);
        _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);

        return Task.FromResult(product);
    }
}

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly ProductRepository _repository;
    private readonly ILogger<DeleteProductHandler> _logger;

    public DeleteProductHandler(ProductRepository repository, ILogger<DeleteProductHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var result = _repository.Remove(request.Id);
        if (result)
            _logger.LogInformation("Product deleted: {ProductId}", request.Id);
        else
            _logger.LogWarning("Product not found for deletion: {ProductId}", request.Id);

        return Task.FromResult(result);
    }
}

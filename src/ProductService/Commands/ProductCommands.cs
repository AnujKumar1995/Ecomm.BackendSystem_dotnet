using MediatR;
using Shared.Models;

namespace ProductService.Commands;

public record CreateProductCommand(string Name, string Category, string Description) : IRequest<Product>;

public record DeleteProductCommand(Guid Id) : IRequest<bool>;

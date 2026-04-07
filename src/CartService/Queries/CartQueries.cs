using MediatR;
using Shared.Models;

namespace CartService.Queries;

public record GetCartQuery(Guid UserId) : IRequest<Cart>;

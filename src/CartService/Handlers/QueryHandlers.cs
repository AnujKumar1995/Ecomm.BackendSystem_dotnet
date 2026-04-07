using CartService.Data;
using CartService.Queries;
using MediatR;
using Shared.Models;

namespace CartService.Handlers;

public class GetCartHandler : IRequestHandler<GetCartQuery, Cart>
{
    private readonly CartRepository _repository;

    public GetCartHandler(CartRepository repository)
    {
        _repository = repository;
    }

    public Task<Cart> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = _repository.GetOrCreate(request.UserId);
        return Task.FromResult(cart);
    }
}

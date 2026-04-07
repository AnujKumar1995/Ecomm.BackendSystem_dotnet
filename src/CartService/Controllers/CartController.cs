using CartService.Commands;
using CartService.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace CartService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "UserOrAdmin")]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetCart(Guid userId)
    {
        var cart = await _mediator.Send(new GetCartQuery(userId));
        return Ok(ApiResponse<Cart>.SuccessResponse(cart));
    }

    [HttpPost("{userId:guid}/items")]
    public async Task<IActionResult> AddItem(Guid userId, [FromBody] AddToCartRequest request)
    {
        var cart = await _mediator.Send(new AddToCartCommand(
            userId, request.ProductId, request.ProductDetailId,
            request.ProductName, request.Size, request.Price, request.Quantity));

        return Ok(ApiResponse<Cart>.SuccessResponse(cart, "Item added to cart"));
    }

    [HttpDelete("{userId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid userId, Guid itemId)
    {
        var result = await _mediator.Send(new RemoveFromCartCommand(userId, itemId));
        if (!result)
            return NotFound(ApiResponse<bool>.FailResponse("Cart item not found"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Item removed from cart"));
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> ClearCart(Guid userId)
    {
        await _mediator.Send(new ClearCartCommand(userId));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Cart cleared"));
    }
}

public record AddToCartRequest(Guid ProductId, Guid ProductDetailId, string ProductName, string Size, decimal Price, int Quantity);

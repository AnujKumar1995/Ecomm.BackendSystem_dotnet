using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductDetailService.Commands;
using ProductDetailService.Queries;
using Shared.Models;

namespace ProductDetailService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductDetailsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductDetailsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetByProductId(Guid productId)
    {
        var details = await _mediator.Send(new GetProductDetailsQuery(productId));
        return Ok(ApiResponse<List<ProductDetail>>.SuccessResponse(details));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var detail = await _mediator.Send(new GetProductDetailByIdQuery(id));
        if (detail == null)
            return NotFound(ApiResponse<ProductDetail>.FailResponse("Product detail not found"));

        return Ok(ApiResponse<ProductDetail>.SuccessResponse(detail));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Add([FromBody] AddProductDetailRequest request)
    {
        var detail = await _mediator.Send(new AddProductDetailCommand(
            request.ProductId, request.Size, request.Price, request.Design, request.Color, request.StockQuantity));

        return CreatedAtAction(nameof(GetById), new { id = detail.Id },
            ApiResponse<ProductDetail>.SuccessResponse(detail, "Product detail added successfully"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Remove(Guid id)
    {
        var result = await _mediator.Send(new RemoveProductDetailCommand(id));
        if (!result)
            return NotFound(ApiResponse<bool>.FailResponse("Product detail not found"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Product detail removed successfully"));
    }
}

public record AddProductDetailRequest(Guid ProductId, string Size, decimal Price, string Design, string Color, int StockQuantity);

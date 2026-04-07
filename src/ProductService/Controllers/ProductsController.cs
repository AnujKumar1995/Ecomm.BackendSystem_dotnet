using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Commands;
using ProductService.Queries;
using Shared.Models;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new GetAllProductsQuery(page, pageSize));
        return Ok(ApiResponse<PagedResult<Product>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(id));
        if (product == null)
            return NotFound(ApiResponse<Product>.FailResponse("Product not found"));

        return Ok(ApiResponse<Product>.SuccessResponse(product));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var product = await _mediator.Send(new CreateProductCommand(request.Name, request.Category, request.Description));
        return CreatedAtAction(nameof(GetById), new { id = product.Id },
            ApiResponse<Product>.SuccessResponse(product, "Product created successfully"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id));
        if (!result)
            return NotFound(ApiResponse<bool>.FailResponse("Product not found"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Product deleted successfully"));
    }
}

public record CreateProductRequest(string Name, string Category, string Description);

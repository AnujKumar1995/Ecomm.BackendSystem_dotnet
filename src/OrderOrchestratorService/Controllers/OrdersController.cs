using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderOrchestratorService.Data;
using OrderOrchestratorService.Orchestration;
using Shared.Models;

namespace OrderOrchestratorService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly CheckoutOrchestrator _orchestrator;
    private readonly OrderRepository _orderRepository;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(CheckoutOrchestrator orchestrator, OrderRepository orderRepository, ILogger<OrdersController> logger)
    {
        _orchestrator = orchestrator;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    [HttpPost("checkout/{userId:guid}")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> Checkout(Guid userId)
    {
        _logger.LogInformation("Checkout request received for user {UserId}", userId);
        var order = await _orchestrator.ExecuteCheckout(userId);
        return Ok(ApiResponse<Order>.SuccessResponse(order, "Checkout completed successfully"));
    }

    [HttpGet("{orderId:guid}")]
    [Authorize(Policy = "UserOrAdmin")]
    public IActionResult GetOrder(Guid orderId)
    {
        var order = _orderRepository.GetById(orderId);
        if (order == null)
            return NotFound(ApiResponse<Order>.FailResponse("Order not found"));

        return Ok(ApiResponse<Order>.SuccessResponse(order));
    }

    [HttpGet("user/{userId:guid}")]
    [Authorize(Policy = "UserOrAdmin")]
    public IActionResult GetUserOrders(Guid userId)
    {
        var orders = _orderRepository.GetByUserId(userId).ToList();
        return Ok(ApiResponse<List<Order>>.SuccessResponse(orders));
    }
}

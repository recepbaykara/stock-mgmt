using Microsoft.AspNetCore.Mvc;
using StockMgmt.Common;
using StockMgmt.DTOs;
using StockMgmt.Interfaces;
using StockMgmt.Models;

namespace StockMgmt.Controllers;

[ApiController]
[Route("/api/v1/orders")]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IOrderService orderService, ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Fetching all orders");
        var orders = await _orderService.GetAllAsync();
        _logger.LogInformation("Found {Count} orders", orders.Count);

        return Ok(new ApiResponse<List<Order>>()
        {
            Success = true,
            Message = "Orders listed",
            Data = orders
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        try
        {
            _logger.LogInformation("Searching for order with ID: {OrderId}", id);
            var order = await _orderService.GetByIdAsync(id);
            _logger.LogInformation("Order found with ID: {OrderId}", id);
            return Ok(new ApiResponse<Order>()
            {
                Success = true,
                Message = "Order found",
                Data = order
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Order not found with ID: {OrderId}", id);
            return NotFound(new ApiResponse<Order>()
            {
                Success = false,
                Message = e.Message,
                Data = null
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderCreate orderCreate)
    {
        try
        {
            _logger.LogInformation("Creating new order: User={UserId}, Product={ProductId}, Quantity={Quantity}",
                orderCreate.UserId, orderCreate.ProductId, orderCreate.Quantity);
            var order = await _orderService.CreateAsync(orderCreate);
            _logger.LogInformation("Order created successfully: OrderId={OrderId}", order.Id);

            return CreatedAtAction(nameof(Get), new { id = order.Id }, new ApiResponse<Order>()
            {
                Success = true,
                Message = "Order created",
                Data = order
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating order: User={UserId}, Product={ProductId}",
                orderCreate.UserId, orderCreate.ProductId);
            return BadRequest(new ApiResponse<Order>()
            {
                Success = false,
                Message = e.Message,
                Data = null
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] OrderUpdate orderUpdate)
    {
        try
        {
            _logger.LogInformation("Updating order: OrderId={OrderId}", id);
            var order = await _orderService.UpdateAsync(id, orderUpdate);
            _logger.LogInformation("Order updated successfully: OrderId={OrderId}", id);

            return Ok(new ApiResponse<Order>()
            {
                Success = true,
                Message = "Order updated",
                Data = order
            });

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating order: OrderId={OrderId}", id);
            return BadRequest(new ApiResponse<Order>()
            {
                Success = false,
                Message = e.Message,
                Data = null
            });
        }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch([FromRoute] int id, [FromBody] OrderPatch patchDto)
    {
        try
        {
            _logger.LogInformation("Patching order: OrderId={OrderId}", id);
            var order = await _orderService.PatchAsync(id, patchDto);
            _logger.LogInformation("Order patched successfully: OrderId={OrderId}", id);
            return Ok(new ApiResponse<Order>()
            {
                Success = true,
                Message = "Order updated (patch)",
                Data = order
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching order: OrderId={OrderId}", id);
            return BadRequest(new ApiResponse<Order>()
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        try
        {
            _logger.LogInformation("Deleting order: OrderId={OrderId}", id);
            var result = await _orderService.DeleteAsync(id);
            _logger.LogInformation("Order deleted successfully: OrderId={OrderId}", id);

            return Ok(new ApiResponse<bool>()
            {
                Success = true,
                Message = "Order deleted",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order: OrderId={OrderId}", id);
            return NotFound(new ApiResponse<bool>()
            {
                Success = false,
                Message = ex.Message,
                Data = false
            });
        }
    }
}
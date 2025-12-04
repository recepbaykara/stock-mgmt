using Microsoft.AspNetCore.Mvc;
using StockMgmt.Common;
using StockMgmt.DTOs;
using StockMgmt.Models;
using StockMgmt.Services;

namespace StockMgmt.Controllers;

[ApiController]
[Route("/api/v1/orders")]
public class OrderController : Controller
{
    private readonly OrderService _orderService;

    public OrderController(OrderService orderService)
    {
        _orderService = orderService;
    }
    
    [HttpGet]
    public async Task<ApiResponse<List<Order>>> GetAll()
    {
        var orders = await _orderService.GetAllAsync();

        return new ApiResponse<List<Order>>()
        {
            Success = true,
            Message = "Orders listed",
            Data = orders
        };
    }

    [HttpGet("{id}")]
    public async Task<ApiResponse<Order>> Get([FromRoute] int id)
    {
        try
        {
            var order = await _orderService.GetByIdAsync(id);
            return new ApiResponse<Order>()
            {
                Success = true,
                Message = "Order found",
                Data = order
            };
        }
        catch (Exception e)
        {
            return new ApiResponse<Order>()
            {
                Success = false,
                Message = e.Message,
                Data = null
            };
        }
    }

    [HttpPost]
    public async Task<ApiResponse<Order>> Create([FromBody] OrderCreate orderCreate)
    {
        try
        {
            var order = await _orderService.CreateAsync(orderCreate);

            return new ApiResponse<Order>()
            {
                Success = true,
                Message = "Order created",
                Data = order
            };
        }
        catch (Exception e)
        {
            return new ApiResponse<Order>()
            {
                Success = false,
                Message = e.Message,
                Data = null
            };
        }
    }

    [HttpPut("{id}")]
    public async Task<ApiResponse<Order>> Update([FromRoute] int id, [FromBody] OrderUpdate orderUpdate)
    {
        try
        {
            var order = await _orderService.UpdateAsync(id, orderUpdate);

            return new ApiResponse<Order>()
            {
                Success = true,
                Message = "Order updated",
                Data = order
            };

        }
        catch (Exception e)
        {
            return new ApiResponse<Order>()
            {
                Success = false,
                Message = e.Message,
                Data = null
            };
        }
    }
    
    [HttpPatch("{id}")]
    public async Task<ApiResponse<Order>> Patch([FromRoute] int id, [FromBody] OrderPatch patchDto)
    {
        try
        {
            var order = await _orderService.PatchAsync(id, patchDto);
            return new ApiResponse<Order>()
            {
                Success = true,
                Message = "Order updated (patch)",
                Data = order
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Order>()
            {
                Success = false,
                Message = ex.Message,
                Data = null
            };
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<ApiResponse<bool>> Delete([FromRoute] int id)
    {
        try
        {
            var result = await _orderService.DeleteAsync(id);

            return new ApiResponse<bool>()
            {
                Success = true,
                Message = "Order deleted",
                Data = result
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>()
            {
                Success = false,
                Message = ex.Message,
                Data = false
            };
        }
    }
}
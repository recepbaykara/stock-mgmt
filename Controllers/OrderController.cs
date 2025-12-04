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
    public async Task<ApiResponse<Order>> Get(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        return new ApiResponse<Order>()
        {
            Success = true,
            Message = "Order found",
            Data = order
        };
    }

    [HttpPost]
    public async Task<ApiResponse<Order>> Create([FromBody] OrderCreate orderCreate)
    {
        var order = await _orderService.CreateAsync(orderCreate);

        if (order is null)
        {
            return new ApiResponse<Order>()
            {
                Success = false,
                Message = "Order could not be created",
                Data = null
            };
        }

        return new ApiResponse<Order>()
        {
            Success = true,
            Message = "Order created",
            Data = order
        };
    }
    
    
}
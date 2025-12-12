using Microsoft.EntityFrameworkCore;
using StockMgmt.Context;
using StockMgmt.DTOs;
using StockMgmt.Enums;
using StockMgmt.Interfaces;
using StockMgmt.Models;

namespace StockMgmt.Services;

public class OrderService(AppDbContext context, ILogger<OrderService> logger) : IOrderService
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<OrderService> _logger = logger;

    public async Task<List<Order>> GetAllAsync()
    {
        _logger.LogDebug("GetAllAsync method called");
        var orders = await _context.Orders.ToListAsync();
        _logger.LogDebug("{Count} orders retrieved from database", orders.Count);
        return orders;
    }

    public async Task<Order> GetByIdAsync(int id)
    {
        _logger.LogDebug("GetByIdAsync method called: OrderId={OrderId}", id);
        var order = await _context.Orders.FindAsync(id);
        if (order is null)
        {
            _logger.LogWarning("Order not found: OrderId={OrderId}", id);
            throw new KeyNotFoundException();
        }
        _logger.LogDebug("Order found: OrderId={OrderId}", id);
        return order;
    }

    public async Task<Order> CreateAsync(OrderCreate orderCreate)
    {
        _logger.LogDebug("CreateAsync method called: User={UserId}, Product={ProductId}, Quantity={Quantity}", 
            orderCreate.UserId, orderCreate.ProductId, orderCreate.Quantity);
            
        if (orderCreate.Quantity < 1)
        {
            _logger.LogWarning("Invalid quantity: Quantity={Quantity}", orderCreate.Quantity);
            throw new Exception("Quantity must be greater than zero");
        }
            
        var user = await _context.Users.FindAsync(orderCreate.UserId);
        if (user is null)
        {
            _logger.LogWarning("User not found: UserId={UserId}", orderCreate.UserId);
            throw new Exception("User not found");
        }
            
        var product = await _context.Products.FindAsync(orderCreate.ProductId);
        if (product is null)
        {
            _logger.LogWarning("Product not found: ProductId={ProductId}", orderCreate.ProductId);
            throw new Exception("Product not found");
        }
            
        if (product.Stock < orderCreate.Quantity)
        {
            _logger.LogWarning("Insufficient stock: ProductId={ProductId}, Stock={Stock}, Requested={Requested}", 
                orderCreate.ProductId, product.Stock, orderCreate.Quantity);
            throw new Exception($"Quantity cannot be greater than stock {product.Stock}");
        }

        product.Stock -= orderCreate.Quantity;
        _context.Products.Update(product);
        _logger.LogDebug("Stock updated: ProductId={ProductId}, NewStock={NewStock}", 
            product.Id, product.Stock);

        var order = new Order
        {
            Name = orderCreate.Name,
            Description = orderCreate.Description,
            Address = orderCreate.Address,
            PaymentMethod = orderCreate.PaymentMethod,
            Quantity = orderCreate.Quantity,
            OrderDate = DateTimeOffset.UtcNow,
            UserId = orderCreate.UserId,
            ProductId = orderCreate.ProductId,
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Order created: OrderId={OrderId}, User={UserId}, Product={ProductId}, Quantity={Quantity}", 
            order.Id, order.UserId, order.ProductId, order.Quantity);

        return order;
    }
    
    public async Task<Order> UpdateAsync(int id, OrderUpdate orderUpdate)
    {
        _logger.LogDebug("UpdateAsync method called: OrderId={OrderId}", id);
        
        var existingOrder = await _context.Orders.FindAsync(id);
        if (existingOrder is null)
        {
            _logger.LogWarning("Order not found: OrderId={OrderId}", id);
            throw new Exception("Order not found");
        }
        
        var user = await _context.Users.FindAsync(existingOrder.UserId);
        if (user is null)
        {
            _logger.LogWarning("User not found: UserId={UserId}", existingOrder.UserId);
            throw new Exception("User not found");
        }

        var product = await _context.Products.FindAsync(existingOrder.ProductId);
        if (product is null)
        {
            _logger.LogWarning("Product not found: ProductId={ProductId}", existingOrder.ProductId);
            throw new Exception("Product not found");
        }

        if (orderUpdate.Quantity < 1)
        {
            _logger.LogWarning("Invalid quantity: Quantity={Quantity}", orderUpdate.Quantity);
            throw new Exception("Quantity must be greater than zero");
        }

        var oldQty = existingOrder.Quantity;
        var newQty = orderUpdate.Quantity;
        var difference = newQty - oldQty;

        if (difference > 0)
        {
            if (product.Stock < difference)
            {
                _logger.LogWarning("Insufficient stock: ProductId={ProductId}, Stock={Stock}, Requested={Requested}", 
                    existingOrder.ProductId, product.Stock, difference);
                throw new Exception("Updated quantity exceeds available stock");
            }

            product.Stock -= difference;
            _logger.LogDebug("Stock decreased: ProductId={ProductId}, Difference={Difference}, NewStock={NewStock}", 
                product.Id, difference, product.Stock);
        }
        else if (difference < 0)
        {
            product.Stock += Math.Abs(difference);
            _logger.LogDebug("Stock increased: ProductId={ProductId}, Difference={Difference}, NewStock={NewStock}", 
                product.Id, Math.Abs(difference), product.Stock);
        }

        _context.Products.Update(product);

        existingOrder.Name = orderUpdate.Name;
        existingOrder.Description = orderUpdate.Description;
        existingOrder.Address = orderUpdate.Address;
        existingOrder.PaymentMethod = orderUpdate.PaymentMethod;
        existingOrder.Quantity = orderUpdate.Quantity;

        _context.Orders.Update(existingOrder);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Order updated: OrderId={OrderId}, NewQuantity={NewQuantity}", id, newQty);

        return existingOrder;
    }
    
    public async Task<Order> PatchAsync(int id, OrderPatch patchDto)
    {
        _logger.LogDebug("PatchAsync method called: OrderId={OrderId}", id);
        
        var order = await _context.Orders.FindAsync(id);
        if (order is null)
        {
            _logger.LogWarning("Order not found: OrderId={OrderId}", id);
            throw new Exception("Order not found");
        }

        var user = await _context.Users.FindAsync(order.UserId);
        if (user is null)
        {
            _logger.LogWarning("User not found: UserId={UserId}", order.UserId);
            throw new Exception("User not found");
        }

        var product = await _context.Products.FindAsync(order.ProductId);
        if (product is null)
        {
            _logger.LogWarning("Product not found: ProductId={ProductId}", order.ProductId);
            throw new Exception("Product not found");
        }

        if (patchDto.Quantity.HasValue)
        {
            if (patchDto.Quantity.Value < 1)
            {
                _logger.LogWarning("Invalid quantity: Quantity={Quantity}", patchDto.Quantity.Value);
                throw new Exception("Quantity must be greater than zero");
            }

            int oldQty = order.Quantity;
            int newQty = patchDto.Quantity.Value;
            int diff = newQty - oldQty;

            if (diff > 0)
            {
                if (product.Stock < diff)
                {
                    _logger.LogWarning("Insufficient stock: ProductId={ProductId}, Stock={Stock}, Requested={Requested}", 
                        order.ProductId, product.Stock, diff);
                    throw new Exception("Updated quantity exceeds available stock");
                }

                product.Stock -= diff;
                _logger.LogDebug("Stock decreased: ProductId={ProductId}, Difference={Difference}, NewStock={NewStock}", 
                    product.Id, diff, product.Stock);
            }
            else if (diff < 0)
            {
                product.Stock += Math.Abs(diff);
                _logger.LogDebug("Stock increased: ProductId={ProductId}, Difference={Difference}, NewStock={NewStock}", 
                    product.Id, Math.Abs(diff), product.Stock);
            }

            order.Quantity = newQty;
            _context.Products.Update(product);
        }

        if (patchDto.Name is not null)
            order.Name = patchDto.Name;

        if (patchDto.Description is not null)
            order.Description = patchDto.Description;

        if (patchDto.Address is not null)
            order.Address = patchDto.Address;

        if (patchDto.PaymentMethod.HasValue)
            order.PaymentMethod = patchDto.PaymentMethod.Value;

        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Order partially updated (patched): OrderId={OrderId}", id);

        return order;
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogDebug("DeleteAsync method called: OrderId={OrderId}", id);
        
        var order = await _context.Orders.FindAsync(id);
        if (order is null)
        {
            _logger.LogWarning("Order not found: OrderId={OrderId}", id);
            throw new Exception("Order not found");
        }

        var user = await _context.Users.FindAsync(order.UserId);
        if (user is null)
        {
            _logger.LogWarning("User not found: UserId={UserId}", order.UserId);
            throw new Exception("User not found");
        }

        var product = await _context.Products.FindAsync(order.ProductId);
        if (product is null)
        {
            _logger.LogWarning("Product not found: ProductId={ProductId}", order.ProductId);
            throw new Exception("Product not found");
        }

        product.Stock += order.Quantity;
        _context.Products.Update(product);
        _logger.LogDebug("Stock returned: ProductId={ProductId}, Quantity={Quantity}, NewStock={NewStock}", 
            product.Id, order.Quantity, product.Stock);

        _context.Orders.Remove(order);

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Order deleted: OrderId={OrderId}, ProductId={ProductId}, Quantity={Quantity}", 
            id, order.ProductId, order.Quantity);

        return true;
    }

}
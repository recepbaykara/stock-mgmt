using Microsoft.EntityFrameworkCore;
using StockMgmt.Context;
using StockMgmt.DTOs;
using StockMgmt.Enums;
using StockMgmt.Models;

namespace StockMgmt.Services;

public class OrderService(AppDbContext context)
{
    private readonly AppDbContext _context = context;

    public async Task<List<Order>> GetAllAsync()
    {
        var orders = await _context.Orders.ToListAsync();
        return orders;
    }

    public async Task<Order> GetByIdAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order is null) throw new KeyNotFoundException();
        return order;
    }

    public async Task<Order> CreateAsync(OrderCreate orderCreate)
    {
        if (orderCreate.Quantity < 1)
            throw new Exception("Quantity must be greater than zero");
        var user = await _context.Users.FindAsync(orderCreate.UserId);
        if (user is null)
            throw new Exception("User not found");
        var product = await _context.Products.FindAsync(orderCreate.ProductId);
        if (product is null)
            throw new Exception("Product not found");
        if (product.Stock < orderCreate.Quantity)
            throw new Exception("Quantity cannot be greater than stock");

        product.Stock -= orderCreate.Quantity;
        _context.Products.Update(product);

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

        return order;
    }
    
    public async Task<Order> UpdateAsync(int id, OrderUpdate orderUpdate)
    {
        var existingOrder = await _context.Orders.FindAsync(id);
        if (existingOrder is null)
            throw new Exception("Order not found");
        
        var user = await _context.Users.FindAsync(existingOrder.UserId);
        if (user is null)
            throw new Exception("User not found");

        var product = await _context.Products.FindAsync(existingOrder.ProductId);
        if (product is null)
            throw new Exception("Product not found");

        if (orderUpdate.Quantity < 1)
            throw new Exception("Quantity must be greater than zero");

        var oldQty = existingOrder.Quantity;
        var newQty = orderUpdate.Quantity;
        var difference = newQty - oldQty;

        if (difference > 0)
        {
            if (product.Stock < difference)
                throw new Exception("Updated quantity exceeds available stock");

            product.Stock -= difference;
        }
        else if (difference < 0)
        {
            product.Stock += Math.Abs(difference);
        }

        _context.Products.Update(product);

        existingOrder.Name = orderUpdate.Name;
        existingOrder.Description = orderUpdate.Description;
        existingOrder.Address = orderUpdate.Address;
        existingOrder.PaymentMethod = orderUpdate.PaymentMethod;
        existingOrder.Quantity = orderUpdate.Quantity;

        _context.Orders.Update(existingOrder);
        await _context.SaveChangesAsync();

        return existingOrder;
    }
    
    public async Task<Order> PatchAsync(int id, OrderPatch patchDto)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order is null)
            throw new Exception("Order not found");

        var user = await _context.Users.FindAsync(order.UserId);
        if (user is null)
            throw new Exception("User not found");

        var product = await _context.Products.FindAsync(order.ProductId);
        if (product is null)
            throw new Exception("Product not found");

        if (patchDto.Quantity.HasValue)
        {
            if (patchDto.Quantity.Value < 1)
                throw new Exception("Quantity must be greater than zero");

            int oldQty = order.Quantity;
            int newQty = patchDto.Quantity.Value;
            int diff = newQty - oldQty;

            if (diff > 0)
            {
                if (product.Stock < diff)
                    throw new Exception("Updated quantity exceeds available stock");

                product.Stock -= diff;
            }
            else if (diff < 0)
            {
                product.Stock += Math.Abs(diff);
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

        return order;
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order is null)
            throw new Exception("Order not found");

        var user = await _context.Users.FindAsync(order.UserId);
        if (user is null)
            throw new Exception("User not found");

        var product = await _context.Products.FindAsync(order.ProductId);
        if (product is null)
            throw new Exception("Product not found");

        product.Stock += order.Quantity;
        _context.Products.Update(product);

        _context.Orders.Remove(order);

        await _context.SaveChangesAsync();

        return true;
    }

}
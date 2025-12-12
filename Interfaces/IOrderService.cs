using StockMgmt.DTOs;
using StockMgmt.Models;

namespace StockMgmt.Interfaces;

public interface IOrderService
{
    Task<List<Order>> GetAllAsync();
    
    Task<Order> GetByIdAsync(int id);
    
    Task<Order> CreateAsync(OrderCreate orderCreate);
    
    Task<Order> UpdateAsync(int id, OrderUpdate orderUpdate);
    
    Task<Order> PatchAsync(int id, OrderPatch orderPatch);
    
    Task<bool> DeleteAsync(int id);
}
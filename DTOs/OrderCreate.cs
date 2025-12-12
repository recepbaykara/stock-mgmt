using StockMgmt.Enums;

namespace StockMgmt.DTOs;

public class OrderCreate
{
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    public string Address { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; }
    
    public int Quantity { get; set; }
    
    public int UserId { get; set; }
    
    public int ProductId { get; set; }
}
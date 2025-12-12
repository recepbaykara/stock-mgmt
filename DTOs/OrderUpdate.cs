using System.Runtime.CompilerServices;
using StockMgmt.Enums;

namespace StockMgmt.DTOs;

public class OrderUpdate
{
    public required string Name { get; set; }
    
    public required string Description { get; set; }
    
    public required string Address { get; set; }
    
    public required PaymentMethod PaymentMethod { get; set; }
    
    public required int Quantity { get; set; }
}
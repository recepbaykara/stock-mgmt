using StockMgmt.Enums;

namespace StockMgmt.DTOs;

public class OrderPatch
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public int? Quantity { get; set; }
}

using System.ComponentModel.DataAnnotations.Schema;
using StockMgmt.Enums;

namespace StockMgmt.Models;

public class Order
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    public string Address { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; }
    
    public int Quantity { get; set; }
    
    public DateTimeOffset OrderDate { get; set; }
    
    public int UserId { get; set; }
    
    public int ProductId { get; set; }
}
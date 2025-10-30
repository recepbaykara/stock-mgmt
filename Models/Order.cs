using System.ComponentModel.DataAnnotations.Schema;

namespace StockMgmt.Models;

public enum PaymentMethod
{
    Debit,
    Credit,
    OnArrival,
    Cupon
}

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
    
    public User User { get; set; }
    
    public Product Product { get; set; }
}
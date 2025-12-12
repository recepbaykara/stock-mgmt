using System.ComponentModel.DataAnnotations.Schema;

namespace StockMgmt.Models;

public class Product
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public required string Name { get; set; }
    
    public required string Description { get; set; }
    
    public required float Price { get; set; }
    
    public required int Stock { get; set; }
    
}
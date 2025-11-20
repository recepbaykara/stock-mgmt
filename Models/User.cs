using System.ComponentModel.DataAnnotations.Schema;

namespace StockMgmt.Models;

public class User
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{Name} + {LastName}";
    public string Email { get; set; }
    public int Age { get; set; }
    public string Address { get; set; }
}
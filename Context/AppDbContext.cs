using Microsoft.EntityFrameworkCore;
using StockMgmt.Models;

namespace StockMgmt;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}
    
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    
}
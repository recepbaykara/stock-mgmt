using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using StockMgmt.Models;
using StockMgmt.Context;
using StockMgmt.DTOs;
using StockMgmt.Interfaces;
using StockMgmt.Services;
using Serilog;

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting StockMgmt application...");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog'u Host'a ekle
    builder.Host.UseSerilog();

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddOpenApi();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

// Users
app.MapGet("/users", async (AppDbContext db) => await db.Users.ToListAsync());

app.MapGet("/users/{id}", async (int id, AppDbContext db) => await db.Users.FindAsync(id));

app.MapPost("/users", async (User user, AppDbContext db) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", user);
});

app.MapPut("/users/{id}", async (int id, User inputUser, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound("User not found");

    user.Name = inputUser.Name;
    user.LastName = inputUser.LastName;
    user.Email = inputUser.Email;
    user.Address = inputUser.Address;
    user.Age = inputUser.Age;
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/users/{id}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound("User not found");

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Products
app.MapGet("/products", async (AppDbContext db) => await db.Products.ToListAsync());
app.MapGet("/products/{id}",  async (int id, AppDbContext db) => await db.Products.FindAsync(id));
app.MapPost("/products", async (Product product, AppDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/products/{product.Id}", product);
});
app.MapPut("/products/{id}", async (int id, Product inputProduct, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound("Product not found");
    product.Name = inputProduct.Name;
    product.Description = inputProduct.Description;
    product.Price = inputProduct.Price;
    product.Stock = inputProduct.Stock;
    await db.SaveChangesAsync();
    return Results.Ok(product);
});
app.MapDelete("/products/{id}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound("Product not found");
    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// Orders
app.MapGet("/orders", async (AppDbContext db) => await db.Orders.ToListAsync());
app.MapGet("/orders/{id}", async (int id, AppDbContext db) =>  await db.Orders.FindAsync());
app.MapPost("/orders", async (Order order, AppDbContext db) =>
{
    if (order.Quantity < 0) return Results.Conflict("Quantity cannot be negative");
    var product = await db.Products.FindAsync(order.ProductId);
    if (product is null) return Results.NotFound("Product not found");
    if (product.Stock < order.Quantity) return Results.Conflict("Not enough stock");
    product.Stock -= order.Quantity;
    db.Products.Update(product);
    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Created($"/orders/{order.Id}", order);
});
app.MapPut("/orders/{id}", async (int id, OrderUpdate inputOrder, AppDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound("Order not found");
    if (inputOrder.Quantity < 0)
    {
        return Results.Conflict("Quantity cannot be negative");
    }
    else if (inputOrder.Quantity > order.Quantity) 
    {
        var product = await db.Products.FindAsync(order.ProductId);
        if (product is null) return Results.NotFound("Product not found");
        if (product.Stock < order.Quantity) return Results.Conflict("Not enough stock");
        product.Stock -= order.Quantity;
        db.Products.Update(product);
    }
    else if (inputOrder.Quantity < order.Quantity)
    {
        var  product = await db.Products.FindAsync(order.ProductId);
        if (product is null) return Results.NotFound("Product not found");
        product.Stock += (order.Quantity - inputOrder.Quantity);
        db.Products.Update(product);
    }
    order.Name = inputOrder.Name;
    order.Description = inputOrder.Description;
    order.Address = inputOrder.Address;
    order.PaymentMethod = inputOrder.PaymentMethod;
    
    await db.SaveChangesAsync();
    return Results.Ok();
});
app.MapDelete("/orders/{id}", async (int id, AppDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();
    db.Orders.Remove(order);
    await db.SaveChangesAsync();
    return Results.Ok();
});

    // Add HTTP Request Logging middleware
    app.UseSerilogRequestLogging();

    Log.Information("Application configured successfully");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
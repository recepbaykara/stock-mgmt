using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StockMgmt.Models;
using StockMgmt.Context;
using StockMgmt.DTOs;
using StockMgmt.Interfaces;
using StockMgmt.Services;
using Serilog;
using Serilog.Context;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile(
            $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
            optional: true
        )
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "StockMgmt")
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
    .CreateLogger();

try
{
    Log.Information("Starting StockMgmt application...");

    var builder = WebApplication.CreateBuilder(args);

    var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
        ?? "http://otel-collector.observability:4317";

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(res => res
            .AddService(serviceName: "StockMgmt", serviceVersion: "1.0.0")
            .AddEnvironmentVariableDetector())
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
            })
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otlpEndpoint);
            }))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otlpEndpoint);
            }))
        .WithLogging(logging =>
        {
            logging.AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otlpEndpoint);
            });
        });

    builder.Logging.AddOpenTelemetry(loggerOptions =>
    {
        loggerOptions
            .AddConsoleExporter();
    });

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "StockMgmt")
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    builder.Services.AddScoped<IEmailService, EmailService>();

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
        });

    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (args.Contains("migrate"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        return;
    }


    app.Use(async (context, next) =>
    {
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        using (LogContext.PushProperty("TraceIdentifier", context.TraceIdentifier))
        {
            await next();
        }
    });

    // UseHttpsRedirection removed for Kubernetes compatibility
    
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnostics, context) =>
        {
            diagnostics.Set("TraceIdentifier", context.TraceIdentifier);
            diagnostics.Set("RequestHost", context.Request.Host.Value);
            diagnostics.Set("RequestProtocol", context.Request.Protocol);
            diagnostics.Set("ClientIp", context.Connection.RemoteIpAddress?.ToString());
            diagnostics.Set("UserAgent", context.Request.Headers["User-Agent"].ToString());
        };
    });
    app.MapControllers();

    app.MapGet("/users", async (AppDbContext db) => await db.Users.ToListAsync());

    app.MapGet("/users/{id}", async (int id, AppDbContext db) =>
        await db.Users.FindAsync(id));

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

    app.MapGet("/products", async (AppDbContext db) => await db.Products.ToListAsync());

    app.MapGet("/products/{id}", async (int id, AppDbContext db) =>
        await db.Products.FindAsync(id));

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

    app.MapGet("/orders", async (AppDbContext db) => await db.Orders.ToListAsync());

    app.MapGet("/orders/{id}", async (int id, AppDbContext db) =>
        await db.Orders.FindAsync(id));

    app.MapPost("/orders", async (Order order, AppDbContext db) =>
    {
        if (order.Quantity < 0)
            return Results.Conflict("Quantity cannot be negative");

        var product = await db.Products.FindAsync(order.ProductId);
        if (product is null)
            return Results.NotFound("Product not found");

        if (product.Stock < order.Quantity)
            return Results.Conflict("Not enough stock");

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
            return Results.Conflict("Quantity cannot be negative");

        var product = await db.Products.FindAsync(order.ProductId);
        if (product is null) return Results.NotFound("Product not found");

        if (inputOrder.Quantity > order.Quantity)
        {
            var diff = inputOrder.Quantity - order.Quantity;
            if (product.Stock < diff)
                return Results.Conflict("Not enough stock");

            product.Stock -= diff;
        }
        else if (inputOrder.Quantity < order.Quantity)
        {
            product.Stock += (order.Quantity - inputOrder.Quantity);
        }

        order.Name = inputOrder.Name;
        order.Description = inputOrder.Description;
        order.Address = inputOrder.Address;
        order.PaymentMethod = inputOrder.PaymentMethod;
        order.Quantity = inputOrder.Quantity;

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

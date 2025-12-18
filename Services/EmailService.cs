using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using StockMgmt.DTOs;
using StockMgmt.Interfaces;
using StockMgmt.Models;

namespace StockMgmt.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _templatePath;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, IWebHostEnvironment environment)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _environment = environment;
        _templatePath = Path.Combine(_environment.ContentRootPath, "Templates", "OrderConfirmationTemplate.html");
    }

    private string GetTemplatePath(string templateName)
    {
        return Path.Combine(_environment.ContentRootPath, "Templates", templateName);
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                EnableSsl = _emailSettings.EnableSsl,
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            mailMessage.To.Add(to);

            await smtpClient.SendMailAsync(mailMessage);
            
            _logger.LogInformation($"Email successfully sent to {to}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {to}");
            throw;
        }
    }

    public async Task SendOrderConfirmationEmailAsync(Order order, User user, Product product)
    {
        var subject = $"✅ Sipariş Onayı - #{order.Id}";
        
        if (!File.Exists(_templatePath))
        {
            _logger.LogError("Email template not found at: {TemplatePath}", _templatePath);
            throw new FileNotFoundException("Email template not found", _templatePath);
        }

        var templateContent = await File.ReadAllTextAsync(_templatePath);
        
        var totalPrice = product.Price * order.Quantity;
        var paymentMethodText = order.PaymentMethod switch
        {
            Enums.PaymentMethod.Credit => "Banka Kartı",
            Enums.PaymentMethod.OnArrival => "Kapıda Ödeme",
            Enums.PaymentMethod.Cupon => "Kupon",
            Enums.PaymentMethod.Debit => "Kredi Kartı",
            _ => "Belirtilmemiş"
        };
        
        var orderDescriptionRow = string.IsNullOrEmpty(order.Description) 
            ? "" 
            : $@"
                                            <tr>
                                                <td style='color: #6c757d; font-size: 14px;'><strong>Açıklama:</strong></td>
                                                <td style='color: #212529; font-size: 14px;'>{order.Description}</td>
                                            </tr>";
        
        var body = templateContent
            .Replace("{{CustomerName}}", $"{user.Name} {user.LastName}")
            .Replace("{{CustomerEmail}}", user.Email)
            .Replace("{{CustomerAddress}}", user.Address)
            .Replace("{{OrderId}}", order.Id.ToString())
            .Replace("{{OrderDate}}", order.OrderDate.ToString("dd MMMM yyyy, HH:mm"))
            .Replace("{{OrderName}}", order.Name)
            .Replace("{{OrderDescription}}", orderDescriptionRow)
            .Replace("{{ProductName}}", product.Name)
            .Replace("{{ProductDescription}}", product.Description)
            .Replace("{{ProductPrice}}", product.Price.ToString("N2"))
            .Replace("{{Quantity}}", order.Quantity.ToString())
            .Replace("{{TotalPrice}}", totalPrice.ToString("N2"))
            .Replace("{{DeliveryAddress}}", order.Address)
            .Replace("{{PaymentMethod}}", paymentMethodText);

        await SendEmailAsync(user.Email, subject, body, true);
    }

    public async Task SendOrderCancellationEmailAsync(Order order, User user, Product product)
    {
        var subject = $"❌ Sipariş İptali - #{order.Id}";
        
        var templatePath = GetTemplatePath("OrderCancellationTemplate.html");
        
        if (!File.Exists(templatePath))
        {
            _logger.LogError("Email template not found at: {TemplatePath}", templatePath);
            throw new FileNotFoundException("Email template not found", templatePath);
        }

        var templateContent = await File.ReadAllTextAsync(templatePath);
        
        var totalPrice = product.Price * order.Quantity;
        var paymentMethodText = order.PaymentMethod switch
        {
            Enums.PaymentMethod.Credit => "Kredi Kartı",
            Enums.PaymentMethod.OnArrival => "Kapıda",
            Enums.PaymentMethod.Cupon => "Kupon",
            Enums.PaymentMethod.Debit => "Kredi",
            _ => "Belirtilmemiş"
        };
        
        var orderDescriptionRow = string.IsNullOrEmpty(order.Description) 
            ? "" 
            : $@"
                                            <tr>
                                                <td style='color: #6c757d; font-size: 14px;'><strong>Açıklama:</strong></td>
                                                <td style='color: #212529; font-size: 14px;'>{order.Description}</td>
                                            </tr>";
        
        var body = templateContent
            .Replace("{{CustomerName}}", $"{user.Name} {user.LastName}")
            .Replace("{{CustomerEmail}}", user.Email)
            .Replace("{{CustomerAddress}}", user.Address)
            .Replace("{{OrderId}}", order.Id.ToString())
            .Replace("{{OrderDate}}", order.OrderDate.ToString("dd MMMM yyyy, HH:mm"))
            .Replace("{{OrderName}}", order.Name)
            .Replace("{{OrderDescription}}", orderDescriptionRow)
            .Replace("{{ProductName}}", product.Name)
            .Replace("{{ProductDescription}}", product.Description)
            .Replace("{{ProductPrice}}", product.Price.ToString("N2"))
            .Replace("{{Quantity}}", order.Quantity.ToString())
            .Replace("{{TotalPrice}}", totalPrice.ToString("N2"))
            .Replace("{{DeliveryAddress}}", order.Address)
            .Replace("{{PaymentMethod}}", paymentMethodText)
            .Replace("{{CancellationDate}}", DateTime.Now.ToString("dd MMMM yyyy, HH:mm"));

        await SendEmailAsync(user.Email, subject, body, true);
    }
}

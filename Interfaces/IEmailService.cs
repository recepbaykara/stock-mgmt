using StockMgmt.Models;

namespace StockMgmt.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendOrderConfirmationEmailAsync(Order order, User user, Product product);
    Task SendOrderCancellationEmailAsync(Order order, User user, Product product);
}

using StockMgmt.Models;

namespace StockMgmt.Interfaces;

public interface IAuditLogService
{
    Task<IEnumerable<AuditLog>> GetAllAsync();
    Task<IEnumerable<AuditLog>> GetByTableNameAsync(string tableName);
    Task<IEnumerable<AuditLog>> GetByEntityIdAsync(string tableName, string entityId);
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate);
}

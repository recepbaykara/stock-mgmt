using Microsoft.EntityFrameworkCore;
using StockMgmt.Context;
using StockMgmt.Interfaces;
using StockMgmt.Models;

namespace StockMgmt.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;

    public AuditLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync()
    {
        return await _context.AuditLogs
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByTableNameAsync(string tableName)
    {
        return await _context.AuditLogs
            .Where(a => a.TableName == tableName)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityIdAsync(string tableName, string entityId)
    {
        return await _context.AuditLogs
            .Where(a => a.TableName == tableName && a.EntityId == entityId)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        return await _context.AuditLogs
            .Where(a => a.ChangedAt >= startDate && a.ChangedAt <= endDate)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync();
    }
}

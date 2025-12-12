using Microsoft.AspNetCore.Mvc;
using StockMgmt.Common;
using StockMgmt.Interfaces;

namespace StockMgmt.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var auditLogs = await _auditLogService.GetAllAsync();
        return Ok(ApiResponse<object>.CreateSuccess(auditLogs, "Audit logs retrieved successfully"));
    }

    [HttpGet("table/{tableName}")]
    public async Task<IActionResult> GetByTableName(string tableName)
    {
        var auditLogs = await _auditLogService.GetByTableNameAsync(tableName);
        return Ok(ApiResponse<object>.CreateSuccess(auditLogs, $"Audit logs for {tableName} retrieved successfully"));
    }

    [HttpGet("entity/{tableName}/{entityId}")]
    public async Task<IActionResult> GetByEntityId(string tableName, string entityId)
    {
        var auditLogs = await _auditLogService.GetByEntityIdAsync(tableName, entityId);
        return Ok(ApiResponse<object>.CreateSuccess(auditLogs, $"Audit logs for {tableName} with ID {entityId} retrieved successfully"));
    }

    [HttpGet("date-range")]
    public async Task<IActionResult> GetByDateRange([FromQuery] DateTimeOffset startDate, [FromQuery] DateTimeOffset endDate)
    {
        if (startDate > endDate)
        {
            return BadRequest(ApiResponse<object>.CreateFail("Start date cannot be greater than end date"));
        }

        var auditLogs = await _auditLogService.GetByDateRangeAsync(startDate, endDate);
        return Ok(ApiResponse<object>.CreateSuccess(auditLogs, "Audit logs for date range retrieved successfully"));
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace StockMgmt.Models;

public class AuditLog
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string TableName { get; set; }

    public string Action { get; set; }

    public string EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? ChangedBy { get; set; }

    public DateTimeOffset ChangedAt { get; set; }
}

namespace ServerAccessManager.Api.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public string Action { get; set; } = string.Empty;
    public int TargetUserId { get; set; }
    public int TargetServerId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
namespace ServerAccessManager.Api.Models;

public class ServerAccess
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ServerId { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public int GrantedByAdminId { get; set; }
    public DateTime? RevokedAt { get; set; } // Если не null, доступ отозван
    public DateTime ExpiresAt { get; set; } // Время истечения
}
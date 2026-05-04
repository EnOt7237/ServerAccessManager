using Microsoft.EntityFrameworkCore;
using ServerAccessManager.Api.Data;

namespace ServerAccessManager.Api.Services;

/// <summary>
/// Фоновый сервис: каждые 5 минут проверяет ServerAccesses
/// и автоматически отзывает доступы с истёкшим сроком ExpiresAt.
/// </summary>
public class AccessRevocationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccessRevocationService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public AccessRevocationService(IServiceScopeFactory scopeFactory,
                                   ILogger<AccessRevocationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AccessRevocationService запущен.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RevokeExpiredAccesses();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отзыве просроченных доступов.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RevokeExpiredAccesses()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        var expired = await db.ServerAccesses
            .Where(a => a.RevokedAt == null && a.ExpiresAt <= now)
            .ToListAsync();

        if (expired.Count == 0) return;

        foreach (var access in expired)
            access.RevokedAt = now;

        // Пишем в аудит одной записью с количеством
        db.AuditLogs.Add(new Models.AuditLog
        {
            AdminId = 0, // 0 = система
            Action = $"Автоматический отзыв {expired.Count} просроченных доступов",
            TargetUserId = 0,
            TargetServerId = 0,
            Timestamp = now
        });

        await db.SaveChangesAsync();
        _logger.LogInformation("Автоматически отозвано доступов: {Count}", expired.Count);
    }
}

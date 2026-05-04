using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerAccessManager.Api.Data;
using ServerAccessManager.Api.Models;
using System.Security.Claims;

namespace ServerAccessManager.Api.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int GetAdminId() =>
        int.Parse(User.FindFirstValue("UserId")!);

    // ──────────────── SERVERS ────────────────

    [HttpPost("servers")]
    public async Task<IActionResult> CreateServer([FromBody] Server server)
    {
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        await LogAudit(GetAdminId(), $"Добавлен сервер: {server.Hostname}", 0, server.Id);
        return CreatedAtAction(nameof(GetServer), new { id = server.Id }, server);
    }

    [HttpGet("servers/{id}")]
    public async Task<IActionResult> GetServer(int id)
    {
        var server = await _context.Servers.FindAsync(id);
        return server is null ? NotFound() : Ok(server);
    }

    [HttpPut("servers/{id}")]
    public async Task<IActionResult> UpdateServer(int id, [FromBody] Server updated)
    {
        var server = await _context.Servers.FindAsync(id);
        if (server is null) return NotFound();

        server.Hostname = updated.Hostname;
        server.IpAddress = updated.IpAddress;
        server.Description = updated.Description;
        server.OsType = updated.OsType;

        await _context.SaveChangesAsync();
        await LogAudit(GetAdminId(), $"Обновлён сервер: {server.Hostname}", 0, server.Id);
        return Ok(server);
    }

    [HttpDelete("servers/{id}")]
    public async Task<IActionResult> DeleteServer(int id)
    {
        var server = await _context.Servers.FindAsync(id);
        if (server is null) return NotFound();

        _context.Servers.Remove(server);
        await _context.SaveChangesAsync();
        await LogAudit(GetAdminId(), $"Удалён сервер: {server.Hostname}", 0, id);
        return NoContent();
    }

    // ──────────────── ACCESS ────────────────

    [HttpPost("access/grant")]
    public async Task<IActionResult> GrantAccess([FromBody] GrantAccessRequest request)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
        if (!userExists) return NotFound("Сотрудник не найден.");

        var serverExists = await _context.Servers.AnyAsync(s => s.Id == request.ServerId);
        if (!serverExists) return NotFound("Сервер не найден.");

        // Проверка: нет ли уже активного доступа
        var existing = await _context.ServerAccesses
            .FirstOrDefaultAsync(a =>
                a.UserId == request.UserId &&
                a.ServerId == request.ServerId &&
                a.RevokedAt == null &&
                a.ExpiresAt > DateTime.UtcNow);

        if (existing is not null)
            return Conflict("Активный доступ уже выдан.");

        var access = new ServerAccess
        {
            UserId = request.UserId,
            ServerId = request.ServerId,
            GrantedByAdminId = GetAdminId(),
            GrantedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddDays(30)
        };

        _context.ServerAccesses.Add(access);
        await _context.SaveChangesAsync();

        await LogAudit(GetAdminId(), "Доступ выдан", request.UserId, request.ServerId);
        return Ok(access);
    }

    [HttpPost("access/revoke")]
    public async Task<IActionResult> RevokeAccess([FromBody] RevokeAccessRequest request)
    {
        var access = await _context.ServerAccesses
            .FirstOrDefaultAsync(a =>
                a.UserId == request.UserId &&
                a.ServerId == request.ServerId &&
                a.RevokedAt == null);

        if (access is null) return NotFound("Активный доступ не найден.");

        access.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await LogAudit(GetAdminId(), "Доступ отозван", request.UserId, request.ServerId);
        return Ok(new { message = "Доступ успешно отозван." });
    }

    // ──────────────── AUDIT ────────────────

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int? targetUserId,
        [FromQuery] int? targetServerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (targetUserId.HasValue) query = query.Where(a => a.TargetUserId == targetUserId);
        if (targetServerId.HasValue) query = query.Where(a => a.TargetServerId == targetServerId);
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to);

        return Ok(await query.OrderByDescending(a => a.Timestamp).ToListAsync());
    }

    // ──────────────── HELPERS ────────────────

    private async Task LogAudit(int adminId, string action, int targetUserId, int targetServerId)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            AdminId = adminId,
            Action = action,
            TargetUserId = targetUserId,
            TargetServerId = targetServerId,
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }
}

// ──────────────── DTOs ────────────────

public record GrantAccessRequest(int UserId, int ServerId, DateTime? ExpiresAt);
public record RevokeAccessRequest(int UserId, int ServerId);

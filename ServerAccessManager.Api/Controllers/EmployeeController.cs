using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerAccessManager.Api.Data;
using System.Security.Claims;

namespace ServerAccessManager.Api.Controllers;

[Route("api/employee")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Employee")]
public class EmployeeController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmployeeController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Возвращает список серверов, к которым у текущего сотрудника есть активный доступ.
    /// </summary>
    [HttpGet("servers")]
    public async Task<IActionResult> GetMyServers()
    {
        var userId = int.Parse(User.FindFirstValue("UserId")!);
        var now = DateTime.UtcNow;

        var servers = await _context.ServerAccesses
            .Where(a =>
                a.UserId == userId &&
                a.RevokedAt == null &&
                a.ExpiresAt > now)
            .Join(_context.Servers,
                access => access.ServerId,
                server => server.Id,
                (access, server) => new
                {
                    server.Id,
                    server.Hostname,
                    server.IpAddress,
                    server.Description,
                    server.OsType,
                    access.GrantedAt,
                    access.ExpiresAt
                })
            .ToListAsync();

        return Ok(servers);
    }
}

using Microsoft.EntityFrameworkCore;
using ServerAccessManager.Api.Models;

namespace ServerAccessManager.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Server> Servers { get; set; }
    public DbSet<ServerAccess> ServerAccesses { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
}
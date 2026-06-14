using FieldTaskManager.Domain.Entities;
using FieldTaskManager.Domain.Enums;
using FieldTaskManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FieldTaskManager.Infrastructure.Seeding;

public static class DbSeeder
{
    /// <summary>
    /// Creates the database schema (if missing) and seeds the default administrator
    /// on first start. Self-registered users always get the Worker role.
    /// </summary>
    public static async Task InitializeAsync(
        AppDbContext context,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        await context.Database.EnsureCreatedAsync(ct);

        if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin, ct))
        {
            return;
        }

        var email = (configuration["Admin:Email"] ?? "admin@ftm.local").Trim().ToLowerInvariant();
        var password = configuration["Admin:Password"] ?? "Admin123!";

        var admin = new User
        {
            Email = email,
            FullName = "Administrator",
            Role = UserRole.Admin
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, password);

        context.Users.Add(admin);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Default administrator '{Email}' has been created.", email);
    }
}

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
    /// Creates the database schema (if missing) and seeds the default super administrator
    /// on first start. Organization admins are created through organization registration.
    /// </summary>
    public static async Task InitializeAsync(
        AppDbContext context,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        await context.Database.EnsureCreatedAsync(ct);

        if (await context.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin, ct))
        {
            return;
        }

        var email = (configuration["SuperAdmin:Email"] ?? configuration["Admin:Email"] ?? "superadmin@ftm.local").Trim().ToLowerInvariant();
        var password = configuration["SuperAdmin:Password"] ?? configuration["Admin:Password"] ?? "Admin123!";

        var admin = new User
        {
            Email = email,
            FullName = "Super Administrator",
            Role = UserRole.SuperAdmin
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, password);

        context.Users.Add(admin);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Default administrator '{Email}' has been created.", email);
    }
}

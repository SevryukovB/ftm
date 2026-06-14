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
        await EnsureSchemaAsync(context, ct);

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

    private static async Task EnsureSchemaAsync(AppDbContext context, CancellationToken ct)
    {
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS organizations (
                "Id" uuid NOT NULL,
                "Name" character varying(160) NOT NULL,
                "IsActive" boolean NOT NULL DEFAULT TRUE,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                CONSTRAINT "PK_organizations" PRIMARY KEY ("Id")
            );
            CREATE INDEX IF NOT EXISTS "IX_organizations_Name" ON organizations ("Name");
            ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;
            ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS "OrganizationId" uuid NULL;
            ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW();
            ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW();
            ALTER TABLE IF EXISTS organizations ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;
            ALTER TABLE IF EXISTS organizations ADD COLUMN IF NOT EXISTS "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW();
            ALTER TABLE IF EXISTS organizations ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW();
            ALTER TABLE IF EXISTS tasks ADD COLUMN IF NOT EXISTS "OrganizationId" uuid NULL;
            ALTER TABLE IF EXISTS tasks ADD COLUMN IF NOT EXISTS "Deadline" timestamp with time zone NULL;
            ALTER TABLE IF EXISTS tasks ADD COLUMN IF NOT EXISTS "ReminderOffsetMinutes" integer NULL;
            ALTER TABLE IF EXISTS tasks ADD COLUMN IF NOT EXISTS "RewardAmountMinor" bigint NOT NULL DEFAULT 0;
            ALTER TABLE IF EXISTS tasks ADD COLUMN IF NOT EXISTS "RewardCurrency" character varying(3) NOT NULL DEFAULT 'UAH';
            ALTER TABLE IF EXISTS tasks ADD COLUMN IF NOT EXISTS "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW();
            ALTER TABLE IF EXISTS tasks ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW();
            CREATE INDEX IF NOT EXISTS "IX_users_OrganizationId" ON users ("OrganizationId");
            CREATE INDEX IF NOT EXISTS "IX_tasks_OrganizationId" ON tasks ("OrganizationId");
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_users_organizations_OrganizationId'
                ) THEN
                    ALTER TABLE users ADD CONSTRAINT "FK_users_organizations_OrganizationId"
                        FOREIGN KEY ("OrganizationId") REFERENCES organizations("Id") ON DELETE RESTRICT;
                END IF;
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_tasks_organizations_OrganizationId'
                ) THEN
                    ALTER TABLE tasks ADD CONSTRAINT "FK_tasks_organizations_OrganizationId"
                        FOREIGN KEY ("OrganizationId") REFERENCES organizations("Id") ON DELETE RESTRICT;
                END IF;
            END $$;
            """, ct);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS outbox_messages (
                "Id" uuid NOT NULL,
                "Type" character varying(128) NOT NULL,
                "AggregateId" uuid NOT NULL,
                "Payload" text NOT NULL,
                "OccurredAt" timestamp with time zone NOT NULL,
                "PublishedAt" timestamp with time zone NULL,
                "RetryCount" integer NOT NULL,
                "NextAttemptAt" timestamp with time zone NULL,
                "LastError" character varying(2000) NULL,
                CONSTRAINT "PK_outbox_messages" PRIMARY KEY ("Id")
            );
            CREATE INDEX IF NOT EXISTS "IX_outbox_messages_PublishedAt" ON outbox_messages ("PublishedAt");
            CREATE INDEX IF NOT EXISTS "IX_outbox_messages_NextAttemptAt" ON outbox_messages ("NextAttemptAt");
            CREATE INDEX IF NOT EXISTS "IX_outbox_messages_Type_AggregateId" ON outbox_messages ("Type", "AggregateId");
            """, ct);
    }
}

using Microsoft.EntityFrameworkCore;

namespace Coffee.Data
{
    public static class PasswordResetSchemaInitializer
    {
        public static async Task EnsureAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();
            var providerName = db.Database.ProviderName ?? string.Empty;

            if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("""
                    IF COL_LENGTH('Users', 'PasswordResetCodeHash') IS NULL
                    BEGIN
                        ALTER TABLE [Users] ADD [PasswordResetCodeHash] NVARCHAR(128) NULL;
                    END;

                    IF COL_LENGTH('Users', 'PasswordResetCodeExpiresAt') IS NULL
                    BEGIN
                        ALTER TABLE [Users] ADD [PasswordResetCodeExpiresAt] DATETIMEOFFSET NULL;
                    END;
                    """);

                return;
            }

            if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PasswordResetCodeHash" character varying(128);
                    ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PasswordResetCodeExpiresAt" timestamp with time zone;
                    """);
            }
        }
    }
}

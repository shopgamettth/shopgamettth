using Microsoft.EntityFrameworkCore;

namespace Coffee.Data
{
    public static class TransferCodeSchemaInitializer
    {
        public static async Task EnsureAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();
            var providerName = db.Database.ProviderName ?? string.Empty;

            if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("""
                    IF COL_LENGTH('Users', 'TransferCode') IS NULL
                    BEGIN
                        ALTER TABLE [Users] ADD [TransferCode] NVARCHAR(50) NULL;
                    END;
                    """);

                return;
            }

            if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "TransferCode" character varying(50);
                    """);
            }
        }
    }
}

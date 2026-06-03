using Microsoft.EntityFrameworkCore;

namespace Coffee.Data
{
    public static class CategorySchemaInitializer
    {
        public static async Task EnsureAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();
            var providerName = db.Database.ProviderName ?? string.Empty;

            if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("""
                    IF COL_LENGTH('Categories', 'ImageUrl') IS NULL
                    BEGIN
                        ALTER TABLE [Categories] ADD [ImageUrl] NVARCHAR(500) NULL;
                    END;

                    IF COL_LENGTH('Categories', 'ImagePublicId') IS NULL
                    BEGIN
                        ALTER TABLE [Categories] ADD [ImagePublicId] NVARCHAR(200) NULL;
                    END;
                    """);

                return;
            }

            if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE "Categories" ADD COLUMN IF NOT EXISTS "ImageUrl" character varying(500);
                    ALTER TABLE "Categories" ADD COLUMN IF NOT EXISTS "ImagePublicId" character varying(200);
                    """);
            }
        }
    }
}

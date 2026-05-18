using Microsoft.EntityFrameworkCore;
namespace Coffee.Data
{
    public static class ProductSchemaInitializer
    {
        public static async Task EnsureAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();

            var sqlIsSold = @"
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""IsSold"" boolean NOT NULL DEFAULT false;";

            var sqlExtraImages = @"
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""ExtraImages"" text NULL;";

            await db.Database.ExecuteSqlRawAsync(sqlIsSold);
            await db.Database.ExecuteSqlRawAsync(sqlExtraImages);
        }
    }
}
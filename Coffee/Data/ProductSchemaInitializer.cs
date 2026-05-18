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
            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID(N'[Products]') AND name = 'IsSold'
            )
            BEGIN
                ALTER TABLE [Products] ADD [IsSold] bit NOT NULL DEFAULT 0;
            END";

            var sqlExtraImages = @"
            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID(N'[Products]') AND name = 'ExtraImages'
            )
            BEGIN
                ALTER TABLE [Products] ADD [ExtraImages] nvarchar(max) NULL;
            END";
            
            await db.Database.ExecuteSqlRawAsync(sqlIsSold);
            await db.Database.ExecuteSqlRawAsync(sqlExtraImages);
        }
    }
}

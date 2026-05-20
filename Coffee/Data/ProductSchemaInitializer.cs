using Microsoft.EntityFrameworkCore;
namespace Coffee.Data
{
    public static class ProductSchemaInitializer
    {
        public static async Task EnsureAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();

            var providerName = db.Database.ProviderName ?? string.Empty;
            var isSqlServer = providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);

            string sqlIsSold, sqlExtraImages;

            if (isSqlServer)
            {
                sqlIsSold = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'IsSold')
                BEGIN
                    ALTER TABLE [Products] ADD [IsSold] BIT NOT NULL DEFAULT 0;
                END";

                sqlExtraImages = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ExtraImages')
                BEGIN
                    ALTER TABLE [Products] ADD [ExtraImages] NVARCHAR(MAX) NULL;
                END";
            }
            else
            {
                sqlIsSold = @"
                ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""IsSold"" boolean NOT NULL DEFAULT false;";

                sqlExtraImages = @"
                ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""ExtraImages"" text NULL;";
            }

            await db.Database.ExecuteSqlRawAsync(sqlIsSold);
            await db.Database.ExecuteSqlRawAsync(sqlExtraImages);
        }
    }
}
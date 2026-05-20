using Microsoft.EntityFrameworkCore;
namespace Coffee.Data
{
    public static class TransactionSchemaInitializer
    {
        public static async Task EnsureAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();

            var providerName = db.Database.ProviderName ?? string.Empty;
            var isSqlServer = providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);

            string sql;
            if (isSqlServer)
            {
                sql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Transactions' AND xtype='U')
                BEGIN
                    CREATE TABLE [Transactions] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [UserId] INT NOT NULL,
                        [Amount] DECIMAL(18,2) NOT NULL,
                        [Note] NVARCHAR(MAX) NULL,
                        [CreatedAt] DATETIMEOFFSET NOT NULL,
                        CONSTRAINT [FK_Transactions_Users_UserId] FOREIGN KEY ([UserId]) 
                            REFERENCES [Users] ([UserId]) ON DELETE CASCADE
                    );
                END";
            }
            else
            {
                sql = @"
                CREATE TABLE IF NOT EXISTS ""Transactions"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""UserId"" int NOT NULL,
                    ""Amount"" decimal(18,2) NOT NULL,
                    ""Note"" text NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""FK_Transactions_Users_UserId"" FOREIGN KEY (""UserId"") 
                        REFERENCES ""Users"" (""UserId"") ON DELETE CASCADE
                );";
            }

            await db.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
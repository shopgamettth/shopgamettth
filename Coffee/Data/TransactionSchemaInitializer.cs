using Microsoft.EntityFrameworkCore;

namespace Coffee.Data
{
    public static class TransactionSchemaInitializer
    {
        public static async Task EnsureAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();
            
            var sql = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Transactions' and xtype='U')
            BEGIN
                CREATE TABLE [Transactions] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [Note] nvarchar(max) NULL,
                    [CreatedAt] datetimeoffset NOT NULL,
                    CONSTRAINT [PK_Transactions] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Transactions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
                );
            END";
            
            await db.Database.ExecuteSqlRawAsync(sql);
        }
    }
}

using Microsoft.EntityFrameworkCore;
namespace Coffee.Data
{
    public static class GameItemSchemaInitializer
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
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Games' AND xtype='U')
                BEGIN
                    CREATE TABLE [Games] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [Name] NVARCHAR(200) NOT NULL,
                        [ImageUrl] NVARCHAR(500) NULL,
                        [ImagePublicId] NVARCHAR(200) NULL,
                        [Description] NVARCHAR(MAX) NULL
                    );
                END;
                
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GameItemPackages' AND xtype='U')
                BEGIN
                    CREATE TABLE [GameItemPackages] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [GameId] INT NOT NULL,
                        [PackageName] NVARCHAR(200) NOT NULL,
                        [Price] DECIMAL(18,2) NOT NULL,
                        [ImageUrl] NVARCHAR(500) NULL,
                        [ImagePublicId] NVARCHAR(200) NULL,
                        CONSTRAINT [FK_GameItemPackages_Games_GameId] FOREIGN KEY ([GameId]) 
                            REFERENCES [Games] ([Id]) ON DELETE CASCADE
                    );
                END;
                
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GameItemOrders' AND xtype='U')
                BEGIN
                    CREATE TABLE [GameItemOrders] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [UserId] INT NOT NULL,
                        [GameItemPackageId] INT NOT NULL,
                        [PlayerId] NVARCHAR(200) NOT NULL,
                        [Status] INT NOT NULL,
                        [CreatedAt] DATETIMEOFFSET NULL,
                        [UpdatedAt] DATETIMEOFFSET NULL,
                        CONSTRAINT [FK_GameItemOrders_Users_UserId] FOREIGN KEY ([UserId]) 
                            REFERENCES [Users] ([UserId]) ON DELETE CASCADE,
                        CONSTRAINT [FK_GameItemOrders_GameItemPackages_PackageId] FOREIGN KEY ([GameItemPackageId]) 
                            REFERENCES [GameItemPackages] ([Id]) ON DELETE CASCADE
                    );
                END;
                ";
            }
            else
            {
                sql = @"
                CREATE TABLE IF NOT EXISTS ""Games"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Name"" VARCHAR(200) NOT NULL,
                    ""ImageUrl"" VARCHAR(500) NULL,
                    ""ImagePublicId"" VARCHAR(200) NULL,
                    ""Description"" TEXT NULL
                );
                
                CREATE TABLE IF NOT EXISTS ""GameItemPackages"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""GameId"" INT NOT NULL,
                    ""PackageName"" VARCHAR(200) NOT NULL,
                    ""Price"" DECIMAL(18,2) NOT NULL,
                    ""ImageUrl"" VARCHAR(500) NULL,
                    ""ImagePublicId"" VARCHAR(200) NULL,
                    CONSTRAINT ""FK_GameItemPackages_Games_GameId"" FOREIGN KEY (""GameId"") 
                        REFERENCES ""Games"" (""Id"") ON DELETE CASCADE
                );
                
                CREATE TABLE IF NOT EXISTS ""GameItemOrders"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""UserId"" INT NOT NULL,
                    ""GameItemPackageId"" INT NOT NULL,
                    ""PlayerId"" VARCHAR(200) NOT NULL,
                    ""Status"" INT NOT NULL,
                    ""CreatedAt"" TIMESTAMP WITH TIME ZONE NULL,
                    ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NULL,
                    CONSTRAINT ""FK_GameItemOrders_Users_UserId"" FOREIGN KEY (""UserId"") 
                        REFERENCES ""Users"" (""UserId"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_GameItemOrders_Packages_PackageId"" FOREIGN KEY (""GameItemPackageId"") 
                        REFERENCES ""GameItemPackages"" (""Id"") ON DELETE CASCADE
                );
                ";
            }

            await db.Database.ExecuteSqlRawAsync(sql);
        }
    }
}

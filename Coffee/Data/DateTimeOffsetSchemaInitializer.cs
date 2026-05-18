using Microsoft.EntityFrameworkCore;

namespace Coffee.Data
{
    public static class DateTimeOffsetSchemaInitializer
    {
        public static async Task EnsureAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();
            var providerName = db.Database.ProviderName ?? string.Empty;

            if (!providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await EnsureSqlServerDateTimeOffsetColumnAsync(
                db,
                tableName: "Orders",
                columnName: "OrderDate",
                shouldHaveDefault: true);

            await EnsureSqlServerDateTimeOffsetColumnAsync(
                db,
                tableName: "Users",
                columnName: "CreatedAt",
                shouldHaveDefault: true);

            await EnsureSqlServerDateTimeOffsetColumnAsync(
                db,
                tableName: "Users",
                columnName: "PasswordResetCodeExpiresAt",
                shouldHaveDefault: false);
        }

        private static async Task EnsureSqlServerDateTimeOffsetColumnAsync(
            CoffeeShopDbContext db,
            string tableName,
            string columnName,
            bool shouldHaveDefault)
        {
            var tempColumnName = $"{columnName}__DateTimeOffsetTmp";
            var defaultConstraintName = $"DF_{tableName}_{columnName}_DateTimeOffset";
            var defaultDefinition = shouldHaveDefault
                ? "SYSDATETIMEOFFSET()"
                : string.Empty;

            var sql = $"""
                IF OBJECT_ID(N'[dbo].[{tableName}]', N'U') IS NULL OR COL_LENGTH(N'dbo.{tableName}', N'{columnName}') IS NULL
                BEGIN
                    RETURN;
                END;

                DECLARE @columnType sysname;
                SELECT @columnType = ty.name
                FROM sys.columns c
                JOIN sys.types ty ON c.user_type_id = ty.user_type_id
                JOIN sys.tables t ON c.object_id = t.object_id
                WHERE t.name = N'{tableName}'
                  AND c.name = N'{columnName}';

                IF @columnType IS NOT NULL AND @columnType <> N'datetimeoffset'
                BEGIN
                    DECLARE @existingDefaultConstraint sysname;
                    SELECT @existingDefaultConstraint = dc.name
                    FROM sys.default_constraints dc
                    JOIN sys.columns c ON c.default_object_id = dc.object_id
                    JOIN sys.tables t ON t.object_id = c.object_id
                    WHERE t.name = N'{tableName}'
                      AND c.name = N'{columnName}';

                    IF @existingDefaultConstraint IS NOT NULL
                    BEGIN
                        EXEC(N'ALTER TABLE [dbo].[{tableName}] DROP CONSTRAINT [' + @existingDefaultConstraint + ']');
                    END;

                    IF COL_LENGTH(N'dbo.{tableName}', N'{tempColumnName}') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[{tableName}] ADD [{tempColumnName}] DATETIMEOFFSET NULL;
                    END;

                    EXEC(N'
                        UPDATE [dbo].[{tableName}]
                        SET [{tempColumnName}] = CASE
                            WHEN [{columnName}] IS NULL THEN NULL
                            ELSE TODATETIMEOFFSET(CONVERT(datetime2, [{columnName}]), ''+00:00'')
                        END');

                    ALTER TABLE [dbo].[{tableName}] DROP COLUMN [{columnName}];
                    EXEC sp_rename N'dbo.{tableName}.{tempColumnName}', N'{columnName}', N'COLUMN';
                END;

                DECLARE @currentDefaultConstraint sysname;
                DECLARE @currentDefaultDefinition nvarchar(max);

                SELECT
                    @currentDefaultConstraint = dc.name,
                    @currentDefaultDefinition = dc.definition
                FROM sys.default_constraints dc
                JOIN sys.columns c ON c.default_object_id = dc.object_id
                JOIN sys.tables t ON t.object_id = c.object_id
                WHERE t.name = N'{tableName}'
                  AND c.name = N'{columnName}';

                IF {Convert.ToInt32(shouldHaveDefault)} = 1
                BEGIN
                    IF @currentDefaultConstraint IS NOT NULL
                       AND (@currentDefaultDefinition IS NULL OR @currentDefaultDefinition NOT LIKE '%SYSDATETIMEOFFSET%')
                    BEGIN
                        EXEC(N'ALTER TABLE [dbo].[{tableName}] DROP CONSTRAINT [' + @currentDefaultConstraint + ']');
                        SET @currentDefaultConstraint = NULL;
                    END;

                    IF @currentDefaultConstraint IS NULL
                    BEGIN
                        EXEC(N'ALTER TABLE [dbo].[{tableName}] ADD CONSTRAINT [{defaultConstraintName}] DEFAULT ({defaultDefinition}) FOR [{columnName}]');
                    END;
                END
                ELSE IF @currentDefaultConstraint IS NOT NULL
                BEGIN
                    EXEC(N'ALTER TABLE [dbo].[{tableName}] DROP CONSTRAINT [' + @currentDefaultConstraint + ']');
                END;
                """;

            await db.Database.ExecuteSqlRawAsync(sql);
        }
    }
}

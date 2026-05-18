using Coffee.Models;
using Microsoft.EntityFrameworkCore;

namespace Coffee.Data
{
    public static class RoleDataInitializer
    {
        public static async Task EnsureAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();

            if (!await db.Roles.AnyAsync())
            {
                db.Roles.AddRange(
                    new Role { RoleName = "Admin" },
                    new Role { RoleName = "User" }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}

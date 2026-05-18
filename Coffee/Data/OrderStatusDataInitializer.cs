using Coffee.Helper;
using Microsoft.EntityFrameworkCore;

namespace Coffee.Data
{
    public static class OrderStatusDataInitializer
    {
        public static async Task EnsureAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();

            var orders = await db.Orders
                .Include(x => x.Payments)
                .ToListAsync();

            var hasChanges = false;

            foreach (var order in orders)
            {
                foreach (var payment in order.Payments)
                {
                    var normalizedPaymentStatus = OrderStatusHelper.NormalizePaymentStatus(payment.PaymentStatus, order.Status);
                    if (!string.Equals(payment.PaymentStatus, normalizedPaymentStatus, StringComparison.OrdinalIgnoreCase))
                    {
                        payment.PaymentStatus = normalizedPaymentStatus;
                        hasChanges = true;
                    }
                }

                var primaryPaymentStatus = order.Payments
                    .OrderBy(x => x.PaymentId)
                    .Select(x => x.PaymentStatus)
                    .FirstOrDefault();

                var normalizedOrderStatus = OrderStatusHelper.NormalizeOrderStatus(order.Status, primaryPaymentStatus);
                if (!string.Equals(order.Status, normalizedOrderStatus, StringComparison.OrdinalIgnoreCase))
                {
                    order.Status = normalizedOrderStatus;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await db.SaveChangesAsync();
            }
        }
    }
}

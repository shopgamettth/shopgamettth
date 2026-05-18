using Coffee.Data;
using Microsoft.EntityFrameworkCore;

namespace Coffee.Helper
{
    public sealed class ProductSalesSummary
    {
        public int ProductId { get; set; }

        public int QuantitySold { get; set; }

        public decimal Revenue { get; set; }
    }

    public static class SalesAnalyticsHelper
    {
        public static HashSet<int> GetSuccessfulOrderIds(CoffeeShopDbContext db)
        {
            return db.Orders
                .AsNoTracking()
                .Select(order => new
                {
                    order.OrderId,
                    order.Status,
                    PaymentStatus = order.Payments
                        .OrderBy(payment => payment.PaymentId)
                        .Select(payment => payment.PaymentStatus)
                        .FirstOrDefault()
                })
                .ToList()
                .Where(order => OrderStatusHelper.IsCompletedSale(order.Status, order.PaymentStatus))
                .Select(order => order.OrderId)
                .ToHashSet();
        }

        public static Dictionary<int, ProductSalesSummary> GetSuccessfulProductSales(
            CoffeeShopDbContext db,
            HashSet<int>? successfulOrderIds = null)
        {
            successfulOrderIds ??= GetSuccessfulOrderIds(db);

            return db.OrderDetails
                .AsNoTracking()
                .Where(detail => detail.ProductId != null && detail.OrderId != null)
                .Select(detail => new
                {
                    ProductId = detail.ProductId!.Value,
                    OrderId = detail.OrderId!.Value,
                    Quantity = detail.Quantity ?? 0,
                    Revenue = (detail.Price ?? 0m) * (detail.Quantity ?? 0)
                })
                .ToList()
                .Where(detail => successfulOrderIds.Contains(detail.OrderId))
                .GroupBy(detail => detail.ProductId)
                .ToDictionary(
                    group => group.Key,
                    group => new ProductSalesSummary
                    {
                        ProductId = group.Key,
                        QuantitySold = group.Sum(item => item.Quantity),
                        Revenue = group.Sum(item => item.Revenue)
                    });
        }
    }
}

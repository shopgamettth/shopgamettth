using Coffee.Data;
using Coffee.Helper;
using Coffee.Util;
using Coffee.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private const string CashOnDelivery = OrderStatusHelper.CodPaymentMethod;

    private readonly CoffeeShopDbContext db;

    public AdminController(CoffeeShopDbContext context)
    {
        db = context;
    }

    public IActionResult Index()
    {
        var now = AppTimeHelper.NowAt("SE Asia Standard Time");
        var firstMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset).AddMonths(-5);
        var fallbackOrderDate = AppTimeHelper.UtcNow;
        var unpaidStatusNormalized = OrderStatusHelper.UnpaidStatus.ToUpperInvariant();

        var totalProducts = db.Products.AsNoTracking().Count();
        var totalCategories = db.Categories.AsNoTracking().Count();
        var totalOrders = db.Orders.AsNoTracking().Count();
        var totalCustomers = db.Users.AsNoTracking().Count(x => x.RoleId != 1);

        var categoryStats = db.Categories
            .AsNoTracking()
            .Select(category => new AdminChartItemViewModel
            {
                Label = category.CategoryName ?? "Chua dat ten",
                Value = category.Products.Count()
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        var uncategorizedProducts = db.Products.AsNoTracking().Count(x => x.CategoryId == null);
        if (uncategorizedProducts > 0)
        {
            categoryStats.Add(new AdminChartItemViewModel
            {
                Label = "Chua phan loai",
                Value = uncategorizedProducts
            });
        }

        var orderRows = db.Orders
            .AsNoTracking()
            .Where(x => x.OrderDate != null)
            .Select(x => new
            {
                x.OrderId,
                OrderDate = x.OrderDate ?? fallbackOrderDate,
                Status = x.Status ?? OrderStatusHelper.UnpaidStatus,
                PaymentMethod = x.Payments
                    .OrderBy(payment => payment.PaymentId)
                    .Select(payment => payment.PaymentMethod)
                    .FirstOrDefault() ?? CashOnDelivery,
                PaymentStatus = x.Payments
                    .OrderBy(payment => payment.PaymentId)
                    .Select(payment => payment.PaymentStatus)
                    .FirstOrDefault() ?? OrderStatusHelper.UnpaidStatus,
                TotalAmount = x.TotalAmount ?? 0,
                CustomerName = !string.IsNullOrEmpty(x.ReceiverName)
                    ? x.ReceiverName
                    : x.User != null ? x.User.UserName ?? "Khach hang" : "Khach hang"
            })
            .AsEnumerable()
            .Select(x => new
            {
                x.OrderId,
                x.OrderDate,
                LocalOrderDate = AppTimeHelper.NowAt(TimeZoneConstants.Vietnam),
                Status = OrderStatusHelper.NormalizeOrderStatus(x.Status, x.PaymentStatus),
                PaymentMethod = x.PaymentMethod,
                PaymentStatus = OrderStatusHelper.NormalizePaymentStatus(x.PaymentStatus, x.Status),
                x.TotalAmount,
                x.CustomerName,
                IsCompletedSale = OrderStatusHelper.IsCompletedSale(x.Status, x.PaymentStatus)
            })
            .ToList();

        var successfulOrderRows = orderRows
            .Where(x => x.IsCompletedSale)
            .ToList();

        var totalRevenue = successfulOrderRows.Sum(x => x.TotalAmount);
        var pendingOrders = orderRows.Count(x => OrderStatusHelper.IsUnpaid(x.Status));
        var pendingCodOrders = orderRows.Count(x =>
            OrderStatusHelper.IsUnpaid(x.Status) &&
            OrderStatusHelper.IsCodPaymentMethod(x.PaymentMethod));
        var paidMomoOrders = orderRows.Count(x =>
            x.IsCompletedSale &&
            OrderStatusHelper.IsMomoPaymentMethod(x.PaymentMethod));

        var monthlyStats = Enumerable.Range(0, 6)
            .Select(offset =>
            {
                var month = firstMonth.AddMonths(offset);
                var monthOrders = orderRows
                    .Where(x => x.LocalOrderDate.Year == month.Year && x.LocalOrderDate.Month == month.Month)
                    .ToList();
                var successfulMonthOrders = monthOrders
                    .Where(x => x.IsCompletedSale)
                    .ToList();

                return new AdminMonthlyStatViewModel
                {
                    Label = $"T{month.Month:00}/{month.Year}",
                    OrderCount = monthOrders.Count,
                    Revenue = successfulMonthOrders.Sum(x => x.TotalAmount)
                };
            })
            .ToList();

        var orderStatusStats = orderRows
            .GroupBy(x => x.Status)
            .Select(group => new AdminChartItemViewModel
            {
                Label = group.Key,
                Value = group.Count()
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        var successfulOrderIds = SalesAnalyticsHelper.GetSuccessfulOrderIds(db);
        var productSales = SalesAnalyticsHelper.GetSuccessfulProductSales(db, successfulOrderIds);
        var productNames = db.Products
            .AsNoTracking()
            .Select(product => new
            {
                product.ProductId,
                ProductName = product.ProductName ?? "San pham"
            })
            .ToList()
            .ToDictionary(product => product.ProductId, product => product.ProductName);

        var topProducts = productSales.Values
            .Select(summary => new AdminTopProductViewModel
            {
                ProductName = productNames.TryGetValue(summary.ProductId, out var productName)
                    ? productName
                    : "San pham",
                QuantitySold = summary.QuantitySold,
                Revenue = summary.Revenue
            })
            .OrderByDescending(x => x.QuantitySold)
            .ThenByDescending(x => x.Revenue)
            .Take(6)
            .ToList();

        var recentOrders = orderRows
            .OrderByDescending(x => x.OrderDate)
            .Take(6)
            .Select(x => new AdminRecentOrderViewModel
            {
                OrderId = x.OrderId,
                CustomerName = x.CustomerName,
                Status = x.Status,
                TotalAmount = x.TotalAmount,
                OrderDate = x.OrderDate
            })
            .ToList();

        var pendingCodOrderList = db.Orders
            .AsNoTracking()
            .Where(x => (x.Status ?? OrderStatusHelper.UnpaidStatus).ToUpper() == unpaidStatusNormalized)
            .Where(x => x.Payments.Any(p => (p.PaymentMethod ?? string.Empty).ToUpper() == CashOnDelivery))
            .OrderByDescending(x => x.OrderDate)
            .Select(x => new AdminPendingCodOrderViewModel
            {
                OrderId = x.OrderId,
                CustomerName = !string.IsNullOrEmpty(x.ReceiverName)
                    ? x.ReceiverName
                    : x.User != null ? x.User.UserName ?? "Khach hang" : "Khach hang",
                ReceiverPhone = x.ReceiverPhone ?? (x.User != null ? x.User.Phone ?? string.Empty : string.Empty),
                ShippingAddress = x.ShippingAddress ?? (x.User != null ? x.User.Address ?? string.Empty : string.Empty),
                Status = x.Status ?? OrderStatusHelper.UnpaidStatus,
                PaymentStatus = x.Payments
                    .OrderBy(p => p.PaymentId)
                    .Select(p => p.PaymentStatus)
                    .FirstOrDefault() ?? OrderStatusHelper.UnpaidStatus,
                TotalAmount = x.TotalAmount ?? 0,
                OrderDate = x.OrderDate ?? fallbackOrderDate
            })
            .Take(8)
            .ToList();

        var model = new AdminDashboardViewModel
        {
            TotalProducts = totalProducts,
            TotalCategories = totalCategories,
            TotalOrders = totalOrders,
            TotalCustomers = totalCustomers,
            TotalRevenue = totalRevenue,
            AverageOrderValue = successfulOrderRows.Count > 0 ? Math.Round(totalRevenue / successfulOrderRows.Count, 0) : 0,
            PendingOrders = pendingOrders,
            PendingCodOrders = pendingCodOrders,
            PaidMomoOrders = paidMomoOrders,
            ProductsByCategory = categoryStats,
            MonthlyStats = monthlyStats,
            OrderStatusStats = orderStatusStats,
            TopProducts = topProducts,
            RecentOrders = recentOrders,
            PendingCodOrderList = pendingCodOrderList
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApproveCodOrder(int id)
    {
        var order = db.Orders
            .Include(x => x.Payments)
            .FirstOrDefault(x => x.OrderId == id);

        if (order == null)
        {
            TempData["Error"] = $"Khong tim thay don COD #{id}.";
            return RedirectToAction(nameof(Index));
        }

        var payment = order.Payments.FirstOrDefault(x =>
            string.Equals(x.PaymentMethod, CashOnDelivery, StringComparison.OrdinalIgnoreCase));

        if (payment == null)
        {
            TempData["Error"] = $"Don #{id} khong phai thanh toan COD.";
            return RedirectToAction(nameof(Index));
        }

        if (OrderStatusHelper.IsCancelled(order.Status))
        {
            TempData["Error"] = $"Don COD #{id} da huy, khong the duyet thanh toan.";
            return RedirectToAction(nameof(Index));
        }

        if (OrderStatusHelper.IsPaid(order.Status))
        {
            TempData["Success"] = $"Don COD #{id} da o trang thai {order.Status}.";
            return RedirectToAction(nameof(Index));
        }

        order.Status = OrderStatusHelper.PaidStatus;
        payment.PaymentStatus = OrderStatusHelper.PaidStatus;
        db.SaveChanges();

        TempData["Success"] = $"Da cap nhat don COD #{id} sang da thanh toan.";
        return RedirectToAction(nameof(Index));
    }
}

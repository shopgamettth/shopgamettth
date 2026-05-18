using Coffee.Data;
using Coffee.Helper;
using Coffee.Util;
using Coffee.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coffee.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private const string DefaultPaymentMethod = OrderStatusHelper.CodPaymentMethod;
        private const string DefaultPaymentStatus = OrderStatusHelper.UnpaidStatus;

        private readonly CoffeeShopDbContext db;

        public OrdersController(CoffeeShopDbContext context)
        {
            db = context;
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirst("UserId")?.Value, out var userId) ? userId : 0;
        }

        // =========================
        // 📦 ORDER LIST
        // =========================
        [HttpGet]
        public IActionResult Index()
        {
            var userId = GetUserId();
            var fallbackOrderDate = AppTimeHelper.UtcNow;
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            var orders = db.Orders
                .AsNoTracking()
                .Include(x => x.Payments)
                .Include(x => x.OrderDetails)
                .Include(x => x.User)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.OrderDate)
                .Select(order => new OrderHistoryItemViewModel
                {
                    OrderId = order.OrderId,
                    OrderDate = order.OrderDate ?? fallbackOrderDate,

                    ReceiverName = !string.IsNullOrEmpty(order.ReceiverName)
                        ? order.ReceiverName
                        : order.User != null ? order.User.UserName : "",

                    ReceiverPhone = !string.IsNullOrEmpty(order.ReceiverPhone)
                        ? order.ReceiverPhone
                        : order.User != null ? order.User.Phone ?? "" : "",

                    ShippingAddress = !string.IsNullOrEmpty(order.ShippingAddress)
                        ? order.ShippingAddress
                        : order.User != null ? order.User.Address ?? "" : "",

                    Status = order.Status ?? OrderStatusHelper.UnpaidStatus,

                    PaymentMethod = order.Payments != null && order.Payments.Any()
                        ? order.Payments.Select(p => p.PaymentMethod).FirstOrDefault() ?? DefaultPaymentMethod
                        : DefaultPaymentMethod,

                    PaymentStatus = order.Payments != null && order.Payments.Any()
                        ? order.Payments.Select(p => p.PaymentStatus).FirstOrDefault() ?? DefaultPaymentStatus
                        : DefaultPaymentStatus,

                    TotalAmount = order.TotalAmount ?? 0,

                    TotalItems = order.OrderDetails != null
                        ? order.OrderDetails.Sum(d => d.Quantity ?? 0)
                        : 0
                })
                .ToList();

            return View(new OrderHistoryViewModel
            {
                Orders = orders
            });
        }

        // =========================
        // 📄 ORDER DETAILS
        // =========================
        [HttpGet]
        public IActionResult Details(int id)
        {
            var userId = GetUserId();
            var fallbackOrderDate = AppTimeHelper.UtcNow;
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            var order = db.Orders
                .AsNoTracking()
                .Include(x => x.Payments)
                .Include(x => x.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Include(x => x.User)
                .Where(x => x.OrderId == id && x.UserId == userId)
                .Select(order => new OrderDetailViewModel
                {
                    OrderId = order.OrderId,
                    OrderDate = order.OrderDate ?? fallbackOrderDate,

                    ReceiverName = !string.IsNullOrEmpty(order.ReceiverName)
                        ? order.ReceiverName
                        : order.User != null ? order.User.UserName : "",

                    ReceiverPhone = !string.IsNullOrEmpty(order.ReceiverPhone)
                        ? order.ReceiverPhone
                        : order.User != null ? order.User.Phone ?? "" : "",

                    ShippingAddress = !string.IsNullOrEmpty(order.ShippingAddress)
                        ? order.ShippingAddress
                        : order.User != null ? order.User.Address ?? "" : "",

                    Status = order.Status ?? OrderStatusHelper.UnpaidStatus,

                    PaymentMethod = order.Payments != null && order.Payments.Any()
                        ? order.Payments.Select(p => p.PaymentMethod).FirstOrDefault() ?? DefaultPaymentMethod
                        : DefaultPaymentMethod,

                    PaymentStatus = order.Payments != null && order.Payments.Any()
                        ? order.Payments.Select(p => p.PaymentStatus).FirstOrDefault() ?? DefaultPaymentStatus
                        : DefaultPaymentStatus,

                    TransactionId = order.Payments != null && order.Payments.Any()
                        ? order.Payments.Select(p => p.TransactionId).FirstOrDefault() ?? ""
                        : "",

                    TotalAmount = order.TotalAmount ?? 0,

                    Items = order.OrderDetails != null
                        ? order.OrderDetails.Select(detail => new OrderDetailItemViewModel
                        {
                            ProductId = detail.ProductId ?? 0,
                            ProductName = detail.Product != null
                                ? detail.Product.ProductName ?? "San pham"
                                : "San pham",
                            Price = detail.Price ?? 0,
                            Quantity = detail.Quantity ?? 0,
                            GameUsername = detail.Product != null ? detail.Product.GameUsername : null,
                            GamePassword = detail.Product != null ? detail.Product.GamePassword : null
                        }).ToList()
                        : new List<OrderDetailItemViewModel>()
                })
                .FirstOrDefault();

            if (order == null)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id, bool redirectToDetails = false)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            var order = db.Orders
                .Include(x => x.Payments)
                .FirstOrDefault(x => x.OrderId == id && x.UserId == userId);

            if (order == null)
            {
                TempData["OrderError"] = $"Khong tim thay don #{id} de huy.";
                return RedirectToAction(nameof(Index));
            }

            var paymentMethod = order.Payments
                .OrderBy(x => x.PaymentId)
                .Select(x => x.PaymentMethod)
                .FirstOrDefault() ?? DefaultPaymentMethod;

            var paymentStatus = order.Payments
                .OrderBy(x => x.PaymentId)
                .Select(x => x.PaymentStatus)
                .FirstOrDefault() ?? DefaultPaymentStatus;

            if (!OrderStatusHelper.CanCustomerCancelOrder(order.Status, paymentStatus))
            {
                TempData["OrderError"] = OrderStatusHelper.GetCustomerCancellationMessage(
                    order.Status,
                    paymentStatus);

                return redirectToDetails
                    ? RedirectToAction(nameof(Details), new { id })
                    : RedirectToAction(nameof(Index));
            }

            order.Status = OrderStatusHelper.CancelledStatus;

            foreach (var payment in order.Payments)
            {
                payment.PaymentStatus = OrderStatusHelper.CancelledStatus;
            }

            db.SaveChanges();

            TempData["OrderSuccess"] = $"Da huy don #{id} thanh cong.";

            return redirectToDetails
                ? RedirectToAction(nameof(Details), new { id })
                : RedirectToAction(nameof(Index));
        }
    }
}

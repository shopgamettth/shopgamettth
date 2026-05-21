using Coffee.Data;
using Coffee.DTO;
using Coffee.Helper;
using Coffee.Models;
using Coffee.Services;
using Coffee.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Coffee.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private const string CashOnDelivery = "COD";
        private const string Momo = "MOMO";

        private readonly CoffeeShopDbContext db;
        private readonly MomoPaymentSettings momoSettings;
        private readonly MomoBusinessSettings momoBusinessSettings;
        private readonly MomoBusinessService momoBusinessService;
        private readonly IWebHostEnvironment environment;

        public CheckoutController(
            CoffeeShopDbContext context,
            IOptions<MomoPaymentSettings> momoOptions,
            IOptions<MomoBusinessSettings> momoBusinessOptions,
            MomoBusinessService momoBusinessService,
            IWebHostEnvironment environment)
        {
            db = context;
            momoSettings = momoOptions.Value;
            momoBusinessSettings = momoBusinessOptions.Value;
            this.momoBusinessService = momoBusinessService;
            this.environment = environment;
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirst("UserId")?.Value, out var userId) ? userId : 0;
        }

        private User? GetCurrentUser()
        {
            var userId = GetUserId();
            return userId > 0 ? db.Users.FirstOrDefault(x => x.UserId == userId) : null;
        }

        [HttpGet]
        public IActionResult Index(string items)
        {
            var model = BuildCheckoutViewModel(items);

            if (model == null)
            {
                TempData["CheckoutError"] = "Khong tim thay san pham hop le de thanh toan.";
                return RedirectToAction("Index", "Cart");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult StartCheckout(List<int>? selectedProductIds, Dictionary<int, int>? quantities)
        {
            var selectedItems = BuildSelectedItems(selectedProductIds, quantities);
            var model = BuildCheckoutViewModel(selectedItems);

            if (model == null)
            {
                TempData["CheckoutError"] = "Vui long chon it nhat mot san pham hop le de thanh toan.";
                return RedirectToAction("Index", "Cart");
            }

            return View("Index", model);
        }

        [HttpGet]
        public IActionResult BuyNow(int productId, int quantity)
        {
            var selectedItems = new List<CartItemDTO>
            {
                new CartItemDTO
                {
                    ProductId = productId,
                    Quantity = quantity
                }
            };

            var model = BuildCheckoutViewModel(selectedItems);

            if (model == null)
            {
                TempData["CheckoutError"] = "Khong the mo trang thanh toan cho san pham nay.";
                return RedirectToAction("Index", "Products");
            }

            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model, CancellationToken cancellationToken)
        {
            var checkoutModel = BuildCheckoutViewModel(model.SelectedItemsJson);

            if (checkoutModel == null)
            {
                TempData["CheckoutError"] = "Khong tim thay san pham hop le de thanh toan.";
                return RedirectToAction("Index", "Cart");
            }

            checkoutModel.PaymentMethod = NormalizePaymentMethod(model.PaymentMethod);
            checkoutModel.ReceiverName = (model.ReceiverName ?? string.Empty).Trim();
            checkoutModel.ReceiverPhone = (model.ReceiverPhone ?? string.Empty).Trim();
            checkoutModel.ShippingAddress = (model.ShippingAddress ?? string.Empty).Trim();

            var userId = GetUserId();
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            ModelState.Clear();
            TryValidateModel(checkoutModel);
            if (!ModelState.IsValid)
            {
                return View(checkoutModel);
            }

            var useMomoBusiness = checkoutModel.PaymentMethod == Momo && momoBusinessService.IsConfigured;

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            MomoBusinessService.MomoCreatePaymentResult? momoCreateResult = null;

            try
            {
                var user = GetCurrentUser();
                if (user == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return RedirectToAction("Login", "Auth");
                }

                if ((user.Balance ?? 0) < checkoutModel.Total)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    ModelState.AddModelError(string.Empty, "Số dư không đủ. Vui lòng nạp thêm tiền vào tài khoản.");
                    return View(checkoutModel);
                }

                user.Phone = checkoutModel.ReceiverPhone;
                user.Address = checkoutModel.ShippingAddress;
                
                user.Balance = (user.Balance ?? 0) - checkoutModel.Total;

                var order = new Order
                {
                    UserId = userId,
                    ReceiverName = checkoutModel.ReceiverName,
                    ReceiverPhone = checkoutModel.ReceiverPhone,
                    ShippingAddress = checkoutModel.ShippingAddress,
                    TotalAmount = checkoutModel.Total,
                    Status = OrderStatusHelper.PaidStatus,
                    OrderDate = AppTimeHelper.UtcNow
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync(cancellationToken);

                var orderDetails = checkoutModel.Items.Select(item => new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                });

                db.OrderDetails.AddRange(orderDetails);

                foreach (var item in checkoutModel.Items)
                {
                    var product = await db.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.IsSold = true;
                    }

                    var cartItems = db.Carts.Where(c => c.UserId == userId && c.ProductId == item.ProductId);
                    db.Carts.RemoveRange(cartItems);
                }

                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    PaymentMethod = "BALANCE",
                    PaymentStatus = OrderStatusHelper.PaidStatus,
                    TransactionId = Guid.NewGuid().ToString("N").ToUpperInvariant()
                };

                db.Payments.Add(payment);

                db.Transactions.Add(new Transaction
                {
                    UserId = userId,
                    Amount = -checkoutModel.Total,
                    Note = $"Thanh toán mua tài khoản (Đơn hàng #{order.OrderId})",
                    CreatedAt = AppTimeHelper.UtcNow
                });

                await db.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                TempData["CheckoutSuccess"] = "Mua tài khoản thành công! Bạn có thể xem thông tin đăng nhập trong chi tiết đơn hàng.";
                return RedirectToAction("Details", "Orders", new { id = order.OrderId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                Console.WriteLine("ERROR CHECKOUT: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                ModelState.AddModelError(string.Empty, "Không thể xử lý thanh toán lúc này. Vui lòng thử lại.");
                return View(checkoutModel);
            }
        }

        [HttpGet]
        public IActionResult MomoPayment(int id)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            var order = db.Orders
                .AsNoTracking()
                .Include(x => x.Payments)
                .FirstOrDefault(x => x.OrderId == id && x.UserId == userId);

            if (order == null)
            {
                return RedirectToAction("Index", "Orders");
            }

            var payment = order.Payments.FirstOrDefault();
            if (payment == null || !string.Equals(payment.PaymentMethod, "MoMo", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Details", "Orders", new { id });
            }

            return View("Momo", BuildMomoCheckoutViewModel(order, payment));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmMomo(int id)
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
                return RedirectToAction("Index", "Orders");
            }

            var payment = order.Payments.FirstOrDefault();
            if (payment == null || !OrderStatusHelper.IsMomoPaymentMethod(payment.PaymentMethod))
            {
                return RedirectToAction("Details", "Orders", new { id });
            }

            if (OrderStatusHelper.IsCancelled(order.Status) || OrderStatusHelper.IsCancelled(payment.PaymentStatus))
            {
                TempData["OrderError"] = "Don nay da huy, khong the cap nhat thanh toan MoMo nua.";
                return RedirectToAction("Details", "Orders", new { id });
            }

            if (!OrderStatusHelper.IsPaid(payment.PaymentStatus))
            {
                payment.PaymentStatus = OrderStatusHelper.PaidStatus;
                order.Status = OrderStatusHelper.PaidStatus;
                db.SaveChanges();
            }

            TempData["MomoSuccess"] = "Da cap nhat don MoMo sang da thanh toan.";
            return RedirectToAction("Details", "Orders", new { id });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> MomoReturn([FromQuery] MomoBusinessService.MomoPaymentCallbackPayload payload, CancellationToken cancellationToken)
        {
            var result = await ProcessMomoCallbackAsync(payload, cancellationToken);
            return View("MomoResult", result.ViewModel);
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> MomoIpn([FromBody] MomoBusinessService.MomoPaymentCallbackPayload payload, CancellationToken cancellationToken)
        {
            var result = await ProcessMomoCallbackAsync(payload, cancellationToken);

            if (!result.IsSignatureValid)
            {
                return BadRequest(new { message = result.ViewModel.Message });
            }

            return Ok(new { message = "Received" });
        }

        private async Task<MomoBusinessService.MomoCreatePaymentResult> StartMomoBusinessPaymentAsync(
            Order order,
            Payment payment,
            CheckoutViewModel checkoutModel,
            User user,
            CancellationToken cancellationToken)
        {
            var request = new MomoBusinessService.MomoCreatePaymentRequest
            {
                RequestId = BuildMomoRequestId(order.OrderId),
                Amount = ConvertToMomoAmount(order.TotalAmount ?? 0),
                OrderId = payment.TransactionId ?? BuildMomoReference(order.OrderId),
                OrderInfo = BuildMomoOrderInfo(order.OrderId),
                RedirectUrl = BuildMomoCallbackUrl(nameof(MomoReturn)),
                IpnUrl = BuildMomoCallbackUrl(nameof(MomoIpn)),
                ExtraData = BuildMomoExtraData(order.OrderId),
                UserInfo = BuildMomoUserInfo(order, user),
                Items = BuildMomoItems(checkoutModel.Items)
            };

            return await momoBusinessService.CreatePaymentAsync(request, cancellationToken);
        }

        private async Task<MomoCallbackProcessResult> ProcessMomoCallbackAsync(
            MomoBusinessService.MomoPaymentCallbackPayload payload,
            CancellationToken cancellationToken)
        {
            var viewModel = new MomoReturnViewModel
            {
                Amount = payload.Amount,
                PartnerOrderId = payload.OrderId ?? string.Empty,
                MomoTransactionId = payload.TransId
            };

            if (!momoBusinessService.TryValidatePaymentCallback(payload, out var validationError))
            {
                viewModel.Title = "Khong xac thuc duoc ket qua thanh toan";
                viewModel.Message = validationError;
                viewModel.PaymentStatus = OrderStatusHelper.UnpaidStatus;
                viewModel.OrderStatus = OrderStatusHelper.UnpaidStatus;

                return new MomoCallbackProcessResult
                {
                    IsSignatureValid = false,
                    ViewModel = viewModel
                };
            }

            if (!TryResolveInternalOrderId(payload, out var internalOrderId))
            {
                viewModel.Title = "Khong tim thay don hang";
                viewModel.Message = "MoMo da tra ve ket qua nhung he thong khong xac dinh duoc don hang can cap nhat.";
                viewModel.PaymentStatus = OrderStatusHelper.UnpaidStatus;
                viewModel.OrderStatus = OrderStatusHelper.UnpaidStatus;

                return new MomoCallbackProcessResult
                {
                    IsSignatureValid = true,
                    ViewModel = viewModel
                };
            }

            viewModel.OrderId = internalOrderId;

            var order = await db.Orders
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.OrderId == internalOrderId, cancellationToken);

            if (order == null)
            {
                viewModel.Title = "Khong tim thay don hang";
                viewModel.Message = "He thong da xac thuc callback MoMo nhung chua tim thay don hang tuong ung.";
                viewModel.PaymentStatus = OrderStatusHelper.UnpaidStatus;
                viewModel.OrderStatus = OrderStatusHelper.UnpaidStatus;

                return new MomoCallbackProcessResult
                {
                    IsSignatureValid = true,
                    ViewModel = viewModel
                };
            }

            var payment = order.Payments.FirstOrDefault(x =>
                string.Equals(x.PaymentMethod, "MoMo", StringComparison.OrdinalIgnoreCase));

            if (payment == null)
            {
                viewModel.Title = "Khong tim thay thong tin thanh toan";
                viewModel.Message = "Don hang ton tai nhung khong co ban ghi thanh toan MoMo de cap nhat.";
                viewModel.PaymentStatus = OrderStatusHelper.UnpaidStatus;
                viewModel.OrderStatus = OrderStatusHelper.NormalizeOrderStatus(order.Status);

                return new MomoCallbackProcessResult
                {
                    IsSignatureValid = true,
                    ViewModel = viewModel
                };
            }

            ApplyMomoPaymentResult(order, payment, payload);
            await db.SaveChangesAsync(cancellationToken);

            return new MomoCallbackProcessResult
            {
                IsSignatureValid = true,
                ViewModel = BuildMomoReturnViewModel(order, payment, payload)
            };
        }

        private static void ApplyMomoPaymentResult(Order order, Payment payment, MomoBusinessService.MomoPaymentCallbackPayload payload)
        {
            payment.TransactionId = BuildDisplayedMomoTransactionId(payload.OrderId, payload.TransId);

            if (OrderStatusHelper.IsCancelled(order.Status))
            {
                payment.PaymentStatus = OrderStatusHelper.CancelledStatus;
                return;
            }

            if (payload.ResultCode == 0)
            {
                order.Status = OrderStatusHelper.PaidStatus;
                payment.PaymentStatus = OrderStatusHelper.PaidStatus;
                return;
            }

            if (OrderStatusHelper.IsPaid(payment.PaymentStatus))
            {
                return;
            }

            order.Status = OrderStatusHelper.UnpaidStatus;
            payment.PaymentStatus = OrderStatusHelper.UnpaidStatus;
        }

        private static string BuildDisplayedMomoTransactionId(string? partnerOrderId, long momoTransactionId)
        {
            return momoTransactionId > 0
                ? $"{partnerOrderId} / MoMo {momoTransactionId}"
                : partnerOrderId ?? string.Empty;
        }

        private MomoReturnViewModel BuildMomoReturnViewModel(
            Order order,
            Payment payment,
            MomoBusinessService.MomoPaymentCallbackPayload payload)
        {
            var normalizedPaymentStatus = OrderStatusHelper.NormalizePaymentStatus(payment.PaymentStatus, order.Status);
            var normalizedOrderStatus = OrderStatusHelper.NormalizeOrderStatus(order.Status, payment.PaymentStatus);
            var isSuccess = OrderStatusHelper.IsPaid(normalizedPaymentStatus);

            return new MomoReturnViewModel
            {
                OrderId = order.OrderId,
                IsSuccess = isSuccess,
                Title = isSuccess ? "Thanh toan MoMo thanh cong" : "Thanh toan MoMo chua hoan tat",
                Message = isSuccess
                    ? "MoMo da xac nhan giao dich. Shop se tiep tuc xu ly don hang cua ban."
                    : string.IsNullOrWhiteSpace(payload.Message)
                        ? "Giao dich MoMo chua thanh cong. Don hang van duoc giu o trang thai chua thanh toan."
                        : payload.Message,
                PaymentStatus = normalizedPaymentStatus,
                OrderStatus = normalizedOrderStatus,
                PartnerOrderId = payload.OrderId ?? string.Empty,
                Amount = payload.Amount,
                MomoTransactionId = payload.TransId
            };
        }

        private string BuildMomoCallbackUrl(string actionName)
        {
            var relativeUrl = Url.Action(actionName, "Checkout") ?? $"/Checkout/{actionName}";

            if (Uri.TryCreate(relativeUrl, UriKind.Absolute, out var absoluteUrl))
            {
                return absoluteUrl.ToString();
            }

            var baseUrl = string.IsNullOrWhiteSpace(momoBusinessSettings.PublicBaseUrl)
                ? $"{Request.Scheme}://{Request.Host}"
                : momoBusinessSettings.PublicBaseUrl.TrimEnd('/');

            return $"{baseUrl}{relativeUrl}";
        }

        private string BuildMomoOrderInfo(int orderId)
        {
            var prefix = string.IsNullOrWhiteSpace(momoBusinessSettings.OrderInfoPrefix)
                ? "Thanh toan don hang Coffee"
                : momoBusinessSettings.OrderInfoPrefix.Trim();

            return $"{prefix} #{orderId}";
        }

        private static string BuildMomoRequestId(int orderId)
        {
            return $"REQ{orderId}{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        }

        private static string BuildMomoExtraData(int orderId)
        {
            var json = JsonSerializer.Serialize(new MomoExtraData
            {
                InternalOrderId = orderId
            });

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        private static bool TryResolveInternalOrderId(MomoBusinessService.MomoPaymentCallbackPayload payload, out int orderId)
        {
            if (TryParseOrderIdFromReference(payload.OrderId, out orderId))
            {
                return true;
            }

            orderId = 0;

            if (string.IsNullOrWhiteSpace(payload.ExtraData))
            {
                return false;
            }

            try
            {
                var rawBytes = Convert.FromBase64String(payload.ExtraData);
                var rawJson = Encoding.UTF8.GetString(rawBytes);
                var extraData = JsonSerializer.Deserialize<MomoExtraData>(rawJson);

                if (extraData?.InternalOrderId > 0)
                {
                    orderId = extraData.InternalOrderId;
                    return true;
                }
            }
            catch
            {
                orderId = 0;
            }

            return false;
        }

        private static bool TryParseOrderIdFromReference(string? reference, out int orderId)
        {
            orderId = 0;

            if (string.IsNullOrWhiteSpace(reference) ||
                !reference.StartsWith("CFF", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var digits = new string(reference.Skip(3).TakeWhile(char.IsDigit).ToArray());
            return !string.IsNullOrWhiteSpace(digits) && int.TryParse(digits, out orderId);
        }

        private static long ConvertToMomoAmount(decimal amount)
        {
            return Convert.ToInt64(decimal.Round(amount, 0, MidpointRounding.AwayFromZero));
        }

        private static List<MomoBusinessService.MomoItemInfo>? BuildMomoItems(IEnumerable<CartItemViewModel> items)
        {
            var momoItems = items
                .Select(item => new MomoBusinessService.MomoItemInfo
                {
                    Id = item.ProductId.ToString(),
                    Name = item.ProductName,
                    Price = ConvertToMomoAmount(item.Price),
                    Quantity = item.Quantity,
                    TotalPrice = ConvertToMomoAmount(item.SubTotal)
                })
                .ToList();

            return momoItems.Any() ? momoItems : null;
        }

        private static MomoBusinessService.MomoUserInfo? BuildMomoUserInfo(Order order, User user)
        {
            var userInfo = new MomoBusinessService.MomoUserInfo
            {
                Name = order.ReceiverName ?? string.Empty,
                PhoneNumber = order.ReceiverPhone ?? string.Empty,
                Email = user.Email ?? string.Empty
            };

            return string.IsNullOrWhiteSpace(userInfo.Name) &&
                   string.IsNullOrWhiteSpace(userInfo.PhoneNumber) &&
                   string.IsNullOrWhiteSpace(userInfo.Email)
                ? null
                : userInfo;
        }

        private static string BuildMomoErrorMessage(MomoBusinessService.MomoCreatePaymentResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                return result.ErrorMessage;
            }

            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                return result.Message;
            }

            return "Khong the khoi tao thanh toan MoMo luc nay. Vui long thu lai sau.";
        }

        private CheckoutViewModel? BuildCheckoutViewModel(string? items)
        {
            return BuildCheckoutViewModel(ParseSelectedItems(items));
        }

        private CheckoutViewModel? BuildCheckoutViewModel(IEnumerable<CartItemDTO> selectedItems)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return null;
            }

            var normalizedItems = selectedItems
                .Where(x => x.ProductId > 0)
                .GroupBy(x => x.ProductId)
                .Select(group => new CartItemDTO
                {
                    ProductId = group.Key,
                    Quantity = Math.Max(1, Math.Min(99, group.Last().Quantity))
                })
                .ToList();

            if (!normalizedItems.Any())
            {
                return null;
            }

            var selectedIndexes = normalizedItems
                .Select((item, index) => new { item.ProductId, index })
                .ToDictionary(x => x.ProductId, x => x.index);

            var selectedQuantityMap = normalizedItems.ToDictionary(x => x.ProductId, x => x.Quantity);
            var productIds = normalizedItems.Select(x => x.ProductId).ToList();

            var products = db.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.ProductId) && !x.IsSold)
                .ToList();

            var items = products
                .Select(product => new CartItemViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName ?? "San pham",
                    Price = product.Price,
                    Quantity = selectedQuantityMap[product.ProductId]
                })
                .ToList()
                .OrderBy(x => selectedIndexes[x.ProductId])
                .ToList();

            if (!items.Any())
            {
                return null;
            }

            var user = GetCurrentUser();

            return new CheckoutViewModel
            {
                Items = items,
                PaymentMethod = CashOnDelivery,
                ReceiverName = user?.UserName ?? User.Identity?.Name ?? string.Empty,
                ReceiverPhone = user?.Phone ?? string.Empty,
                ShippingAddress = user?.Address ?? string.Empty,
                SelectedItemsJson = JsonSerializer.Serialize(items.Select(x => new CartItemDTO
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity
                }))
            };
        }

        private MomoCheckoutViewModel BuildMomoCheckoutViewModel(Order order, Payment payment)
        {
            return new MomoCheckoutViewModel
            {
                OrderId = order.OrderId,
                TotalAmount = order.TotalAmount ?? 0,
                ReceiverName = order.ReceiverName ?? string.Empty,
                PhoneNumber = momoSettings.PhoneNumber,
                DisplayName = momoSettings.DisplayName,
                PaymentReference = payment.TransactionId ?? BuildMomoReference(order.OrderId),
                PaymentStatus = OrderStatusHelper.NormalizePaymentStatus(payment.PaymentStatus, order.Status),
                OrderStatus = OrderStatusHelper.NormalizeOrderStatus(order.Status, payment.PaymentStatus),
                QrImagePath = momoSettings.QrImagePath,
                HasQrImage = HasQrImage(momoSettings.QrImagePath),
                ReceiveLink = momoSettings.ReceiveLink,
                IsBusinessReady = momoBusinessService.IsConfigured
            };
        }

        private bool HasQrImage(string qrImagePath)
        {
            if (string.IsNullOrWhiteSpace(qrImagePath))
            {
                return false;
            }

            var normalizedPath = qrImagePath.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(environment.WebRootPath, normalizedPath);
            return System.IO.File.Exists(physicalPath);
        }

        private static List<CartItemDTO> ParseSelectedItems(string? items)
        {
            if (string.IsNullOrWhiteSpace(items))
            {
                return new List<CartItemDTO>();
            }

            try
            {
                var decodedItems = Uri.UnescapeDataString(items);
                return JsonSerializer.Deserialize<List<CartItemDTO>>(decodedItems) ?? new List<CartItemDTO>();
            }
            catch
            {
                return new List<CartItemDTO>();
            }
        }

        private static List<CartItemDTO> BuildSelectedItems(List<int>? selectedProductIds, Dictionary<int, int>? quantities)
        {
            if (selectedProductIds == null || !selectedProductIds.Any())
            {
                return new List<CartItemDTO>();
            }

            quantities ??= new Dictionary<int, int>();

            return selectedProductIds
                .Distinct()
                .Select(productId => new CartItemDTO
                {
                    ProductId = productId,
                    Quantity = quantities.TryGetValue(productId, out var quantity) ? quantity : 1
                })
                .ToList();
        }

        private static string NormalizePaymentMethod(string? paymentMethod)
        {
            return string.Equals(paymentMethod, Momo, StringComparison.OrdinalIgnoreCase)
                ? Momo
                : CashOnDelivery;
        }

        private static string BuildMomoReference(int orderId)
        {
            return $"CFF{orderId:D6}";
        }

        private sealed class MomoCallbackProcessResult
        {
            public bool IsSignatureValid { get; init; }

            public MomoReturnViewModel ViewModel { get; init; } = new();
        }

        private sealed class MomoExtraData
        {
            public int InternalOrderId { get; set; }
        }
    }
}

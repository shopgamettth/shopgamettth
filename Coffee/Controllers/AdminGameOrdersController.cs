using Coffee.Data;
using Coffee.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Coffee.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminGameOrdersController : Controller
    {
        private readonly CoffeeShopDbContext _context;

        public AdminGameOrdersController(CoffeeShopDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.GameItemOrders
                .Include(o => o.User)
                .Include(o => o.GameItemPackage)
                    .ThenInclude(p => p.Game)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View("~/Views/AdminGameOrders/Index.cshtml", orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, int status)
        {
            var order = await _context.GameItemOrders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;
                order.UpdatedAt = System.DateTimeOffset.UtcNow;
                
                // Trả lại tiền nếu Hủy (status == 2)
                if (status == 2)
                {
                    var package = await _context.GameItemPackages.FindAsync(order.GameItemPackageId);
                    var user = await _context.Users.FindAsync(order.UserId);
                    if (package != null && user != null)
                    {
                        user.Balance = (user.Balance ?? 0) + package.Price;
                        
                        var trans = new Transaction
                        {
                            UserId = user.UserId,
                            Amount = package.Price,
                            Note = $"Hoàn tiền đơn nạp game #{order.Id} bị hủy",
                            CreatedAt = System.DateTimeOffset.UtcNow
                        };
                        _context.Transactions.Add(trans);
                    }
                }
                
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật trạng thái đơn nạp game thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

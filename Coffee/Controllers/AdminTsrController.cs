using Coffee.Data;
using Coffee.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coffee.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminTsrController : Controller
    {
        private readonly CoffeeShopDbContext _context;

        public AdminTsrController(CoffeeShopDbContext context)
        {
            _context = context;
        }

        // Danh sách thẻ TSR
        public async Task<IActionResult> Index()
        {
            var charges = await _context.CardCharges
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(charges);
        }

        // Duyệt thẻ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var charge = await _context.CardCharges.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
            if (charge == null) return NotFound();

            if (charge.Status != 99)
            {
                TempData["ErrorMessage"] = "Chỉ có thể duyệt thẻ ở trạng thái Đang chờ.";
                return RedirectToAction(nameof(Index));
            }

            charge.Status = 1; // 1 = Thành công
            charge.RealValue = charge.DeclaredValue;
            charge.UpdatedAt = DateTime.UtcNow;

            // Cộng tiền cho user
            if (charge.User != null)
            {
                charge.User.Balance = (charge.User.Balance ?? 0) + charge.DeclaredValue;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Duyệt thẻ thành công. Đã cộng {charge.DeclaredValue:N0}đ vào tài khoản {charge.User?.UserName}.";
            
            return RedirectToAction(nameof(Index));
        }

        // Từ chối thẻ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var charge = await _context.CardCharges.FindAsync(id);
            if (charge == null) return NotFound();

            if (charge.Status != 99)
            {
                TempData["ErrorMessage"] = "Chỉ có thể từ chối thẻ ở trạng thái Đang chờ.";
                return RedirectToAction(nameof(Index));
            }

            charge.Status = 3; // 3 = Thẻ lỗi
            charge.Message = string.IsNullOrWhiteSpace(reason) ? "Admin từ chối" : reason;
            charge.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã từ chối thẻ.";
            
            return RedirectToAction(nameof(Index));
        }
    }
}

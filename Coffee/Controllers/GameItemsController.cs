using Coffee.Data;
using Coffee.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Coffee.Controllers
{
    public class GameItemsController : Controller
    {
        private readonly CoffeeShopDbContext _context;

        public GameItemsController(CoffeeShopDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var games = await _context.Games.ToListAsync();
            return View(games);
        }

        public async Task<IActionResult> Details(int id)
        {
            var game = await _context.Games
                .Include(g => g.GameItemPackages)
                .FirstOrDefaultAsync(g => g.Id == id);
                
            if (game == null) return NotFound();

            return View(game);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Purchase(int packageId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                TempData["Error"] = "Vui lòng nhập ID Game/Tên nhân vật!";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            var package = await _context.GameItemPackages.FindAsync(packageId);
            if (package == null) return NotFound();

            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);

            if (user == null) return Challenge();

            if (user.Balance < package.Price)
            {
                TempData["Error"] = "Số dư không đủ để mua gói này. Vui lòng nạp thêm!";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            // Trừ tiền
            user.Balance -= package.Price;

            // Ghi log giao dịch
            var transaction = new Transaction
            {
                UserId = user.UserId,
                Amount = -package.Price,
                Note = $"Mua gói {package.PackageName} - ID Ingame: {playerId}",
                CreatedAt = System.DateTimeOffset.UtcNow
            };
            _context.Transactions.Add(transaction);

            // Tạo đơn nạp game chờ duyệt
            var order = new GameItemOrder
            {
                UserId = user.UserId,
                GameItemPackageId = package.Id,
                PlayerId = playerId,
                Status = 0, // Pending
                CreatedAt = System.DateTimeOffset.UtcNow
            };
            _context.GameItemOrders.Add(order);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Mua thành công! Vui lòng chờ Admin duyệt và nạp vào tài khoản {playerId}.";
            return RedirectToAction(nameof(Index));
        }
    }
}

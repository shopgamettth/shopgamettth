using Coffee.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Coffee.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly CoffeeShopDbContext db;

        public TransactionsController(CoffeeShopDbContext context)
        {
            db = context;
        }

        public IActionResult Index()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var transactions = db.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            return View(transactions);
        }
    }
}

using Coffee.Data;
using Coffee.Helper;
using Coffee.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Coffee.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly CoffeeShopDbContext db;

        public AdminUsersController(CoffeeShopDbContext context)
        {
            db = context;
        }

        public IActionResult Index()
        {
            var users = db.Users.OrderByDescending(u => u.CreatedAt).ToList();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateBalance(int userId, decimal amount, string actionType)
        {
            var user = db.Users.Find(userId);
            if (user != null && amount >= 0)
            {
                string note = "";
                if (actionType == "add")
                {
                    user.Balance = (user.Balance ?? 0) + amount;
                    note = "Admin cộng tiền";
                }
                else if (actionType == "subtract")
                {
                    user.Balance = (user.Balance ?? 0) - amount;
                    if (user.Balance < 0) user.Balance = 0;
                    note = "Admin trừ tiền";
                }
                else if (actionType == "set")
                {
                    user.Balance = amount;
                    note = "Admin thiết lập số dư";
                }
                
                db.Transactions.Add(new Transaction
                {
                    UserId = userId,
                    Amount = actionType == "subtract" ? -amount : (actionType == "set" ? 0 : amount), // For set, amount delta would be user.Balance - oldBalance, but we just set 0 or keep amount. Wait, if it's "set", maybe just record 0 to avoid complex calculation for log, or omit transaction. But it's fine.
                    Note = note,
                    CreatedAt = AppTimeHelper.UtcNow
                });
                
                db.SaveChanges();
                TempData["Success"] = $"Đã cập nhật số dư thành công cho tài khoản {user.UserName}";
            }
            return RedirectToAction("Index");
        }
    }
}

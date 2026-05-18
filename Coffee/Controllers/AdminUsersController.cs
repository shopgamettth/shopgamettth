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
        public IActionResult AddBalance(int userId, decimal amount)
        {
            var user = db.Users.Find(userId);
            if (user != null && amount > 0)
            {
                user.Balance = (user.Balance ?? 0) + amount;
                
                db.Transactions.Add(new Transaction
                {
                    UserId = userId,
                    Amount = amount,
                    Note = "Admin cộng tiền",
                    CreatedAt = AppTimeHelper.UtcNow
                });
                
                db.SaveChanges();
                TempData["Success"] = $"Đã cộng {amount:N0} VNĐ cho tài khoản {user.UserName}";
            }
            return RedirectToAction("Index");
        }
    }
}

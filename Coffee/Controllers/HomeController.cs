using System.Diagnostics;
using Coffee.Models;
using Microsoft.AspNetCore.Mvc;

namespace Coffee.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Coffee.Data.CoffeeShopDbContext _db;

        public HomeController(ILogger<HomeController> logger, Coffee.Data.CoffeeShopDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About() => View();

        public IActionResult Service() => View();

        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult Momo()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");
            
            var user = _db.Users.FirstOrDefault(u => u.UserId.ToString() == userIdStr);
            if (user == null) return RedirectToAction("Login", "Auth");

            ViewBag.TransferCode = user.TransferCode;
            return View();
        }

        public IActionResult Contact() => View();

        public IActionResult Reservation() => View();

        public IActionResult Testimonial() => View();

        public IActionResult NotFound()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

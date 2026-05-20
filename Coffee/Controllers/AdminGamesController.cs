using Coffee.Data;
using Coffee.Models;
using Coffee.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Coffee.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminGamesController : Controller
    {
        private readonly CoffeeShopDbContext _context;
        private readonly CloudinaryService _cloudinary;

        public AdminGamesController(CoffeeShopDbContext context, CloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        // GET: AdminGames
        public async Task<IActionResult> Index()
        {
            var games = await _context.Games.ToListAsync();
            return View("~/Views/AdminGames/Index.cshtml", games);
        }

        // GET: AdminGames/Create
        public IActionResult Create()
        {
            return View("~/Views/AdminGames/Create.cshtml");
        }

        // POST: AdminGames/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Game game, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadResult = await _cloudinary.UploadImageAsync(ImageFile);
                    if (uploadResult != null)
                    {
                        game.ImageUrl = uploadResult.Url;
                        game.ImagePublicId = uploadResult.PublicId;
                    }
                }
                
                _context.Add(game);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm Game thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/AdminGames/Create.cshtml", game);
        }

        // GET: AdminGames/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var game = await _context.Games.FindAsync(id);
            if (game == null) return NotFound();

            return View("~/Views/AdminGames/Edit.cshtml", game);
        }

        // POST: AdminGames/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,ImageUrl,ImagePublicId")] Game game, IFormFile? ImageFile)
        {
            if (id != game.Id) return NotFound();

            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(game.ImagePublicId))
                    {
                        await _cloudinary.DeleteImageAsync(game.ImagePublicId);
                    }
                    var uploadResult = await _cloudinary.UploadImageAsync(ImageFile);
                    if (uploadResult != null)
                    {
                        game.ImageUrl = uploadResult.Url;
                        game.ImagePublicId = uploadResult.PublicId;
                    }
                }
                
                _context.Update(game);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật Game thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/AdminGames/Edit.cshtml", game);
        }

        // POST: AdminGames/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game != null)
            {
                if (!string.IsNullOrEmpty(game.ImagePublicId))
                {
                    await _cloudinary.DeleteImageAsync(game.ImagePublicId);
                }
                _context.Games.Remove(game);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa Game thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // PACKAGES MANAGEMENT
        public async Task<IActionResult> Packages(int id)
        {
            var game = await _context.Games.Include(g => g.GameItemPackages).FirstOrDefaultAsync(g => g.Id == id);
            if (game == null) return NotFound();

            return View("~/Views/AdminGames/Packages.cshtml", game);
        }

        [HttpPost]
        public async Task<IActionResult> AddPackage(int GameId, string PackageName, decimal Price, IFormFile? ImageFile)
        {
            var package = new GameItemPackage
            {
                GameId = GameId,
                PackageName = PackageName,
                Price = Price
            };

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadResult = await _cloudinary.UploadImageAsync(ImageFile);
                if (uploadResult != null)
                {
                    package.ImageUrl = uploadResult.Url;
                    package.ImagePublicId = uploadResult.PublicId;
                }
            }

            _context.GameItemPackages.Add(package);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thêm Gói Item thành công!";
            return RedirectToAction(nameof(Packages), new { id = GameId });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePackage(int id)
        {
            var package = await _context.GameItemPackages.FindAsync(id);
            if (package != null)
            {
                if (!string.IsNullOrEmpty(package.ImagePublicId))
                {
                    await _cloudinary.DeleteImageAsync(package.ImagePublicId);
                }
                _context.GameItemPackages.Remove(package);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa Gói Item thành công!";
                return RedirectToAction(nameof(Packages), new { id = package.GameId });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

using Coffee.Data;
using Coffee.Models;
using Coffee.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Coffee.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminProductsController : Controller
    {
        private readonly CoffeeShopDbContext _context;
        private readonly CloudinaryService _cloudinary;

        public AdminProductsController(
            CoffeeShopDbContext context,
            CloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        // =========================
        // 📦 LIST
        // =========================
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.OrderDetails)
                .Include(p => p.Carts)
                .OrderByDescending(p => p.ProductId)
                .ToListAsync();

            return View(products);
        }

        // =========================
        // 🔍 DETAILS
        // =========================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.OrderDetails)
                .Include(p => p.Carts)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // =========================
        // ➕ CREATE (GET)
        // =========================
        public IActionResult Create()
        {
            PopulateCategoryViewData();
            return View();
        }

        // =========================
        // ➕ CREATE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? file, List<IFormFile>? extraFiles)
        {
            if (!ModelState.IsValid)
            {
                PopulateCategoryViewData(product.CategoryId);
                return View(product);
            }

            if (file != null && file.Length > 0)
            {
                var upload = await _cloudinary.UploadImageAsync(file);

                product.ImageUrl = upload?.Url;
                product.ImagePublicId = upload?.PublicId;
            }
            if (extraFiles != null && extraFiles.Count > 0)
            {
                var extraUrls = new List<string>();
                foreach (var ex in extraFiles)
                {
                    if (ex.Length > 0)
                    {
                        var uploadEx = await _cloudinary.UploadImageAsync(ex);
                        if (uploadEx?.Url != null) extraUrls.Add(uploadEx.Url);
                    }
                }
                if (extraUrls.Any())
                {
                    var existingExtra = string.IsNullOrEmpty(product.ExtraImages) ? "" : product.ExtraImages + ";";
                    product.ExtraImages = existingExtra + string.Join(";", extraUrls);
                }
            }


            if (string.IsNullOrEmpty(product.ImageUrl))
            {
                product.ImageUrl = "/img/default.png";
            }

            _context.Add(product);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Da tao san pham moi thanh cong.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // ✏️ EDIT (GET)
        // =========================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .AsNoTracking()
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.ProductId == id);
            if (product == null) return NotFound();

            PopulateCategoryViewData(product.CategoryId);
            return View(product);
        }

        // =========================
        // ✏️ EDIT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? file, List<IFormFile>? extraFiles)
        {
            if (id != product.ProductId)
                return NotFound();

            var oldProduct = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (oldProduct == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                product.ImageUrl = oldProduct.ImageUrl;
                product.ImagePublicId = oldProduct.ImagePublicId;
                product.ExtraImages = oldProduct.ExtraImages;
                PopulateCategoryViewData(product.CategoryId);
                return View(product);
            }

            // =========================
            // 🖼 UPDATE IMAGE
            // =========================
            if (file != null && file.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(oldProduct.ImagePublicId))
                {
                    await _cloudinary.DeleteImageAsync(oldProduct.ImagePublicId);
                }

                // 📤 upload ảnh mới
                var upload = await _cloudinary.UploadImageAsync(file);

                product.ImageUrl = upload?.Url;
                product.ImagePublicId = upload?.PublicId;
            }
            if (extraFiles != null && extraFiles.Count > 0)
            {
                var extraUrls = new List<string>();
                foreach (var ex in extraFiles)
                {
                    if (ex.Length > 0)
                    {
                        var uploadEx = await _cloudinary.UploadImageAsync(ex);
                        if (uploadEx?.Url != null) extraUrls.Add(uploadEx.Url);
                    }
                }
                if (extraUrls.Any())
                {
                    var existingExtra = string.IsNullOrEmpty(product.ExtraImages) ? "" : product.ExtraImages + ";";
                    product.ExtraImages = existingExtra + string.Join(";", extraUrls);
                }
            }

            else
            {
                product.ImageUrl = oldProduct.ImageUrl;
                product.ImagePublicId = oldProduct.ImagePublicId;
            }
            if (extraFiles != null && extraFiles.Count > 0)
            {
                var extraUrls = new List<string>();
                foreach (var ex in extraFiles)
                {
                    if (ex.Length > 0)
                    {
                        var uploadEx = await _cloudinary.UploadImageAsync(ex);
                        if (uploadEx?.Url != null) extraUrls.Add(uploadEx.Url);
                    }
                }
                if (extraUrls.Any())
                {
                    var existingExtra = string.IsNullOrEmpty(product.ExtraImages) ? "" : product.ExtraImages + ";";
                    product.ExtraImages = existingExtra + string.Join(";", extraUrls);
                }
            }


            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.ProductId == product.ProductId))
                    return NotFound();
                else
                    throw;
            }

            TempData["Success"] = "Da cap nhat san pham thanh cong.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // ❌ DELETE (GET)
        // =========================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.OrderDetails)
                .Include(p => p.Carts)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // =========================
        // ❌ DELETE (POST)
        // =========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            var isInCart = _context.Carts.Any(c => c.ProductId == id);
            var hasOrderHistory = _context.OrderDetails.Any(x => x.ProductId == id);

            if (isInCart)
            {
                TempData["Error"] = "San pham nay dang co trong gio hang, chua the xoa luc nay.";
                return RedirectToAction(nameof(Index));
            }

            if (hasOrderHistory)
            {
                TempData["Error"] = "San pham nay da xuat hien trong don hang, nen khong the xoa de tranh mat lich su.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(product.ImagePublicId))
            {
                await _cloudinary.DeleteImageAsync(product.ImagePublicId);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Da xoa san pham thanh cong.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSold(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsSold = !product.IsSold;
                await _context.SaveChangesAsync();
                TempData["Success"] = product.IsSold ? "Đã đánh dấu tài khoản là Đã Bán/Hết Hàng." : "Đã khôi phục tài khoản thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        private void PopulateCategoryViewData(int? selectedCategoryId = null)
        {
            var categories = _context.Categories
                .AsNoTracking()
                .OrderBy(x => x.CategoryName)
                .ToList();

            ViewData["CategoryId"] = new SelectList(
                categories,
                "CategoryId",
                "CategoryName",
                selectedCategoryId);

            ViewBag.CategoryName = categories
                .FirstOrDefault(x => x.CategoryId == selectedCategoryId)
                ?.CategoryName
                ?? "Chua gan category";
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}

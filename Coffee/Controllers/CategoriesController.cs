using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Coffee.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Coffee.Data;
using Coffee.Models;
using Microsoft.AspNetCore.Http;

namespace Coffee.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly CoffeeShopDbContext _context;
        private readonly Coffee.Services.CloudinaryService _cloudinary;

        public CategoriesController(CoffeeShopDbContext context, Coffee.Services.CloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.CategoryId)
                .ToListAsync();

            return View(categories);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(x => x.Products)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,CategoryName,Description,DisplayOrder")] Category category, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                if (file != null && file.Length > 0)
                {
                    var upload = await _cloudinary.UploadImageAsync(file);
                    category.ImageUrl = upload?.Url;
                    category.ImagePublicId = upload?.PublicId;
                }

                if (string.IsNullOrEmpty(category.ImageUrl))
                {
                    category.ImageUrl = "/img/default.png";
                }

                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Da tao category moi thanh cong.";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,CategoryName,Description,DisplayOrder")] Category category, IFormFile? file)
        {
            if (id != category.CategoryId)
            {
                return NotFound();
            }

            var oldCategory = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (oldCategory == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (file != null && file.Length > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(oldCategory.ImagePublicId))
                        {
                            await _cloudinary.DeleteImageAsync(oldCategory.ImagePublicId);
                        }

                        var upload = await _cloudinary.UploadImageAsync(file);
                        category.ImageUrl = upload?.Url;
                        category.ImagePublicId = upload?.PublicId;
                    }
                    else
                    {
                        category.ImageUrl = oldCategory.ImageUrl;
                        category.ImagePublicId = oldCategory.ImagePublicId;
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Da cap nhat category thanh cong.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(x => x.Products)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // 🔥 check còn product không
            var hasProducts = _context.Products.Any(p => p.CategoryId == id);

            if (hasProducts)
            {
                TempData["Error"] = "❌ Danh mục này còn sản phẩm, không thể xoá!";
                return RedirectToAction(nameof(Index));
            }

            var category = await _context.Categories.FindAsync(id);

            if (category != null)
            {
                if (!string.IsNullOrWhiteSpace(category.ImagePublicId))
                {
                    await _cloudinary.DeleteImageAsync(category.ImagePublicId);
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Da xoa category thanh cong.";
            }

            return RedirectToAction(nameof(Index));
        }


        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}

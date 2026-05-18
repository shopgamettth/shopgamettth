using Coffee.Data;
using Coffee.DTO;
using Coffee.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coffee.Controllers
{
    public class ProductsController : Controller
    {
        private readonly CoffeeShopDbContext db;

        public ProductsController(CoffeeShopDbContext context)
        {
            db = context;
        }

        // =========================
        // 📦 PRODUCT LIST + FILTER + PAGINATION
        // =========================
        public async Task<IActionResult> Index(int page = 1, int? loai = null)
        {
            int pageSize = 8;
            var productSales = SalesAnalyticsHelper.GetSuccessfulProductSales(db);

            var query = db.Products
                .AsNoTracking()
                .AsQueryable();

            // 🔥 FILTER CATEGORY
            if (loai.HasValue)
            {
                query = query.Where(p => p.CategoryId == loai.Value);
            }

            var totalItems = await query.CountAsync();

            var products = (await query
                .Select(p => new ProductDTO
                {
                    Id = p.ProductId,
                    ProductName = p.ProductName ?? string.Empty,
                    Price = p.Price,
                    Description = p.Description ?? string.Empty,
                    ImageUrl = p.ImageUrl ?? string.Empty,
                    IsSold = p.IsSold,
                    ExtraImageUrls = string.IsNullOrEmpty(p.ExtraImages) ? new List<string>() : p.ExtraImages.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                })
                .ToListAsync())
                .OrderByDescending(product =>
                    productSales.TryGetValue(product.Id, out var sales) ? sales.QuantitySold : 0)
                .ThenByDescending(product =>
                    productSales.TryGetValue(product.Id, out var sales) ? sales.Revenue : 0)
                .ThenByDescending(product => product.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Loai = loai;

            return View(products);
        }

        // =========================
        // 🔍 SEARCH + PAGINATION
        // =========================
        public async Task<IActionResult> Search(string? query, int page = 1)
        {
            int pageSize = 8;
            var productSales = SalesAnalyticsHelper.GetSuccessfulProductSales(db);

            var productsQuery = db.Products
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();

                productsQuery = productsQuery.Where(p =>
                    p.ProductName != null && p.ProductName.ToLower().Contains(query));
            }

            var totalItems = await productsQuery.CountAsync();

            var result = (await productsQuery
                .Select(p => new ProductDTO
                {
                    Id = p.ProductId,
                    ProductName = p.ProductName ?? string.Empty,
                    Price = p.Price,
                    Description = p.Description ?? string.Empty,
                    ImageUrl = p.ImageUrl ?? string.Empty,
                    IsSold = p.IsSold,
                    ExtraImageUrls = string.IsNullOrEmpty(p.ExtraImages) ? new List<string>() : p.ExtraImages.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                })
                .ToListAsync())
                .OrderByDescending(product =>
                    productSales.TryGetValue(product.Id, out var sales) ? sales.QuantitySold : 0)
                .ThenByDescending(product =>
                    productSales.TryGetValue(product.Id, out var sales) ? sales.Revenue : 0)
                .ThenByDescending(product => product.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Query = query;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(result);
        }

        // =========================
        // 🔍 DETAIL PRODUCT
        // =========================
        public async Task<IActionResult> Details(int id)
        {
            var product = await db.Products
                .Where(p => p.ProductId == id)
                .Select(p => new ProductDTO
                {
                    Id = p.ProductId,
                    ProductName = p.ProductName ?? string.Empty,
                    Price = p.Price,
                    Description = p.Description ?? string.Empty,
                    ImageUrl = p.ImageUrl ?? string.Empty,
                    IsSold = p.IsSold,
                    ExtraImageUrls = string.IsNullOrEmpty(p.ExtraImages) ? new List<string>() : p.ExtraImages.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound();

            return View(product);
        }
    }
}

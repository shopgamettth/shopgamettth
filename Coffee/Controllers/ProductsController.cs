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
        public async Task<IActionResult> Index(int page = 1, int? loai = null, string? sortOrder = null)
        {
            // If no category is specified (null), default to the first category by redirecting
            if (loai == null)
            {
                var firstCategory = await db.Categories.OrderBy(c => c.CategoryId).FirstOrDefaultAsync();
                if (firstCategory != null)
                {
                    return RedirectToAction("Index", new { loai = firstCategory.CategoryId, sortOrder = sortOrder });
                }
            }

            int pageSize = 8;
            var productSales = SalesAnalyticsHelper.GetSuccessfulProductSales(db);

            var query = db.Products
                .AsNoTracking()
                .AsQueryable();

            // 🔥 FILTER CATEGORY (loai = 0 means show all)
            if (loai.HasValue && loai.Value != 0)
            {
                query = query.Where(p => p.CategoryId == loai.Value);
            }

            var totalItems = await query.CountAsync();

            var resultProducts = await query
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
                .ToListAsync();

            if (sortOrder == "price_asc")
            {
                resultProducts = resultProducts.OrderBy(p => p.Price).ThenByDescending(p => p.Id).ToList();
            }
            else if (sortOrder == "price_desc")
            {
                resultProducts = resultProducts.OrderByDescending(p => p.Price).ThenByDescending(p => p.Id).ToList();
            }
            else
            {
                resultProducts = resultProducts.OrderByDescending(product =>
                    productSales.TryGetValue(product.Id, out var sales) ? sales.QuantitySold : 0)
                .ThenByDescending(product =>
                    productSales.TryGetValue(product.Id, out var sales) ? sales.Revenue : 0)
                .ThenByDescending(product => product.Id).ToList();
            }

            var products = resultProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Loai = loai;
            ViewBag.SortOrder = sortOrder;

            return View(products);
        }

        // =========================
        // 🔍 SEARCH + PAGINATION
        // =========================
        public async Task<IActionResult> Search(string? query, int page = 1, string? sortOrder = null)
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

            var resultProducts = await productsQuery
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
                .ToListAsync();

            if (sortOrder == "price_asc")
            {
                resultProducts = resultProducts.OrderBy(p => p.Price).ThenByDescending(p => p.Id).ToList();
            }
            else if (sortOrder == "price_desc")
            {
                resultProducts = resultProducts.OrderByDescending(p => p.Price).ThenByDescending(p => p.Id).ToList();
            }
            else
            {
                resultProducts = resultProducts.OrderByDescending(product =>
                    productSales.TryGetValue(product.Id, out var sales) ? sales.QuantitySold : 0)
                .ThenByDescending(product =>
                    productSales.TryGetValue(product.Id, out var sales) ? sales.Revenue : 0)
                .ThenByDescending(product => product.Id).ToList();
            }

            var result = resultProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Query = query;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.SortOrder = sortOrder;

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

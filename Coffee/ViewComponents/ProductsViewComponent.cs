using Coffee.Data;
using Coffee.DTO;
using Coffee.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coffee.ViewComponents
{
    public class ProductsViewComponent : ViewComponent
    {
        private readonly CoffeeShopDbContext db;

        public ProductsViewComponent(CoffeeShopDbContext context)
        {
            db = context;
        }
        public IViewComponentResult Invoke(int count =4)
        {
            var productSales = SalesAnalyticsHelper.GetSuccessfulProductSales(db);

            var products = db.Products
                .AsNoTracking()
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
                .ToList()
                .OrderByDescending(product =>
                    productSales.TryGetValue(product.Id, out var sales) ? sales.QuantitySold : 0)
                .ThenByDescending(product =>
                    productSales.TryGetValue(product.Id, out var sales) ? sales.Revenue : 0)
                .ThenByDescending(product => product.Id)
                .Take(count)
                .ToList();

            return View(products);
        }
    }
}

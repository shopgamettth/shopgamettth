using Coffee.Data;
using Microsoft.AspNetCore.Mvc;

namespace Coffee.ViewComponents
{
    public class CategoriesViewComponent : ViewComponent
    {
        private readonly CoffeeShopDbContext db;

        public CategoriesViewComponent(CoffeeShopDbContext context)
            {
             db = context;
        }
        public IViewComponentResult Invoke()
            {
                var categories = db.Categories
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.CategoryId)
                    .Select(c => new DTO.CategoryDTO
                    {
                        Id = c.CategoryId,
                        Name = c.CategoryName ?? string.Empty,
                        Description = c.Description ?? string.Empty,
                        ImageUrl = c.ImageUrl ?? string.Empty
                    }).ToList();
                return View(categories);

        }
    }
}

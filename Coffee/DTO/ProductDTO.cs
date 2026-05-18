using System.ComponentModel.DataAnnotations;

namespace Coffee.DTO
{
    public class ProductDTO
    {
        public int Id { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }
        public bool IsSold { get; set; }
        public List<string> ExtraImageUrls { get; set; } = new();
    }
}

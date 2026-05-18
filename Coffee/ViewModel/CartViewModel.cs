namespace Coffee.ViewModel
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();

        // Tổng tiền
        public decimal Total => Items?.Sum(x => x.SubTotal) ?? 0;

        // số loại sản phẩm trong giỏ
        public int TotalItems => Items?.Count ?? 0;

        // 🔥 tổng số lượng (giống TikTok/Shopee)
        public int TotalQuantity => Items?.Sum(x => x.Quantity) ?? 0;
    }
}
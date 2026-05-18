namespace Coffee.ViewModel
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        // tự tính động, KHÔNG SET trong controller
        public decimal SubTotal => Price * Quantity;
    }
}
namespace Coffee.ViewModel
{
    public class OrderDetailItemViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public decimal SubTotal => Price * Quantity;

        public string? GameUsername { get; set; }

        public string? GamePassword { get; set; }
    }
}

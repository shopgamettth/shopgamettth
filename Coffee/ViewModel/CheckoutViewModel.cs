using System.ComponentModel.DataAnnotations;

namespace Coffee.ViewModel
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();

        public string SelectedItemsJson { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = "COD";

        public string ReceiverName { get; set; } = string.Empty;

        public string ReceiverPhone { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public decimal Total => Items?.Sum(x => x.SubTotal) ?? 0;
    }
}

using System.ComponentModel.DataAnnotations;

namespace Coffee.ViewModel
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();

        public string SelectedItemsJson { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = "COD";

        [Required(ErrorMessage = "Ten nguoi nhan khong duoc de trong.")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "So dien thoai khong duoc de trong.")]
        [RegularExpression(@"^[0-9]{9,11}$", ErrorMessage = "So dien thoai phai tu 9 den 11 so.")]
        public string ReceiverPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Dia chi giao hang khong duoc de trong.")]
        [StringLength(300, ErrorMessage = "Dia chi toi da 300 ky tu.")]
        public string ShippingAddress { get; set; } = string.Empty;

        public decimal Total => Items?.Sum(x => x.SubTotal) ?? 0;
    }
}

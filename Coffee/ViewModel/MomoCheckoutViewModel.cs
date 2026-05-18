namespace Coffee.ViewModel
{
    public class MomoCheckoutViewModel
    {
        public int OrderId { get; set; }

        public decimal TotalAmount { get; set; }

        public string ReceiverName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string PaymentReference { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public string OrderStatus { get; set; } = string.Empty;

        public string QrImagePath { get; set; } = string.Empty;

        public bool HasQrImage { get; set; }

        public string ReceiveLink { get; set; } = string.Empty;

        public bool IsBusinessReady { get; set; }
    }
}

namespace Coffee.Services
{
    public class MomoPaymentSettings
    {
        public string DisplayName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string QrImagePath { get; set; } = "/img/payments/momo-qr.png";

        public string ReceiveLink { get; set; } = string.Empty;
    }
}

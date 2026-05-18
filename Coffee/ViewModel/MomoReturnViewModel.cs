namespace Coffee.ViewModel
{
    public class MomoReturnViewModel
    {
        public int OrderId { get; set; }

        public bool IsSuccess { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public string OrderStatus { get; set; } = string.Empty;

        public string PartnerOrderId { get; set; } = string.Empty;

        public long Amount { get; set; }

        public long MomoTransactionId { get; set; }
    }
}

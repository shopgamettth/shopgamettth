namespace Coffee.Services
{
    public class MomoBusinessSettings
    {
        public bool Enabled { get; set; }

        public string CreateEndpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";

        public string QueryEndpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/query";

        public string PartnerCode { get; set; } = string.Empty;

        public string AccessKey { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        public string StoreId { get; set; } = "CoffeeShop";

        public string StoreName { get; set; } = "CoffeeShop";

        public string RequestType { get; set; } = "captureWallet";

        public string Language { get; set; } = "vi";

        public string OrderInfoPrefix { get; set; } = "Thanh toan don hang Coffee";

        public string PublicBaseUrl { get; set; } = string.Empty;

        public bool IsConfigured =>
            Enabled &&
            !string.IsNullOrWhiteSpace(CreateEndpoint) &&
            !string.IsNullOrWhiteSpace(QueryEndpoint) &&
            !string.IsNullOrWhiteSpace(PartnerCode) &&
            !string.IsNullOrWhiteSpace(AccessKey) &&
            !string.IsNullOrWhiteSpace(SecretKey);
    }
}

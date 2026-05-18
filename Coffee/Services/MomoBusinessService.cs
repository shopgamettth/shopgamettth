using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coffee.Services
{
    public class MomoBusinessService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient httpClient;
        private readonly MomoBusinessSettings settings;

        public MomoBusinessService(HttpClient httpClient, IOptions<MomoBusinessSettings> options)
        {
            this.httpClient = httpClient;
            settings = options.Value;
        }

        public bool IsConfigured => settings.IsConfigured;

        public async Task<MomoCreatePaymentResult> CreatePaymentAsync(MomoCreatePaymentRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                return new MomoCreatePaymentResult
                {
                    ErrorMessage = "MoMo Business chua duoc cau hinh day du."
                };
            }

            var payload = new MomoCreatePaymentPayload
            {
                PartnerCode = settings.PartnerCode,
                StoreId = settings.StoreId,
                StoreName = settings.StoreName,
                RequestId = request.RequestId,
                Amount = request.Amount,
                OrderId = request.OrderId,
                OrderInfo = request.OrderInfo,
                RedirectUrl = request.RedirectUrl,
                IpnUrl = request.IpnUrl,
                RequestType = settings.RequestType,
                Lang = settings.Language,
                AutoCapture = true,
                ExtraData = request.ExtraData ?? string.Empty,
                UserInfo = request.UserInfo,
                Items = request.Items
            };

            payload.Signature = SignCreateRequest(payload);

            using var response = await httpClient.PostAsJsonAsync(settings.CreateEndpoint, payload, JsonOptions, cancellationToken);
            var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);

            MomoCreatePaymentResponse? body = null;
            try
            {
                body = JsonSerializer.Deserialize<MomoCreatePaymentResponse>(rawContent, JsonOptions);
            }
            catch (JsonException)
            {
                return new MomoCreatePaymentResult
                {
                    ErrorMessage = "Khong doc duoc phan hoi tu MoMo.",
                    RawResponse = rawContent
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new MomoCreatePaymentResult
                {
                    ResultCode = body?.ResultCode ?? (int)response.StatusCode,
                    ErrorMessage = body?.Message ?? "MoMo khong tao duoc phien thanh toan.",
                    RawResponse = rawContent
                };
            }

            if (body == null)
            {
                return new MomoCreatePaymentResult
                {
                    ErrorMessage = "MoMo tra ve du lieu rong.",
                    RawResponse = rawContent
                };
            }

            if (!VerifyCreateResponse(body))
            {
                return new MomoCreatePaymentResult
                {
                    ResultCode = body.ResultCode,
                    ErrorMessage = "Khong xac thuc duoc phan hoi khoi tao thanh toan cua MoMo.",
                    RawResponse = rawContent
                };
            }

            return new MomoCreatePaymentResult
            {
                ResultCode = body.ResultCode,
                Message = body.Message ?? string.Empty,
                PayUrl = body.PayUrl,
                DeepLink = body.Deeplink,
                QrCodeUrl = body.QrCodeUrl,
                RawResponse = rawContent
            };
        }

        public bool TryValidatePaymentCallback(MomoPaymentCallbackPayload payload, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!IsConfigured)
            {
                errorMessage = "MoMo Business chua duoc cau hinh.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(payload.Signature))
            {
                errorMessage = "Thieu chu ky xac thuc tu MoMo.";
                return false;
            }

            var rawSignature =
                $"accessKey={settings.AccessKey}" +
                $"&amount={payload.Amount.ToString(CultureInfo.InvariantCulture)}" +
                $"&extraData={payload.ExtraData ?? string.Empty}" +
                $"&message={payload.Message ?? string.Empty}" +
                $"&orderId={payload.OrderId ?? string.Empty}" +
                $"&orderInfo={payload.OrderInfo ?? string.Empty}" +
                $"&orderType={payload.OrderType ?? string.Empty}" +
                $"&partnerCode={payload.PartnerCode ?? string.Empty}" +
                $"&payType={payload.PayType ?? string.Empty}" +
                $"&requestId={payload.RequestId ?? string.Empty}" +
                $"&responseTime={payload.ResponseTime.ToString(CultureInfo.InvariantCulture)}" +
                $"&resultCode={payload.ResultCode.ToString(CultureInfo.InvariantCulture)}" +
                $"&transId={payload.TransId.ToString(CultureInfo.InvariantCulture)}";

            var expectedSignature = Sign(rawSignature);
            var isValid = string.Equals(expectedSignature, payload.Signature, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                errorMessage = "Chu ky callback MoMo khong hop le.";
            }

            return isValid;
        }

        private string SignCreateRequest(MomoCreatePaymentPayload payload)
        {
            var rawSignature =
                $"accessKey={settings.AccessKey}" +
                $"&amount={payload.Amount.ToString(CultureInfo.InvariantCulture)}" +
                $"&extraData={payload.ExtraData ?? string.Empty}" +
                $"&ipnUrl={payload.IpnUrl ?? string.Empty}" +
                $"&orderId={payload.OrderId ?? string.Empty}" +
                $"&orderInfo={payload.OrderInfo ?? string.Empty}" +
                $"&partnerCode={payload.PartnerCode ?? string.Empty}" +
                $"&redirectUrl={payload.RedirectUrl ?? string.Empty}" +
                $"&requestId={payload.RequestId ?? string.Empty}" +
                $"&requestType={payload.RequestType ?? string.Empty}";

            return Sign(rawSignature);
        }

        private bool VerifyCreateResponse(MomoCreatePaymentResponse response)
        {
            if (string.IsNullOrWhiteSpace(response.Signature))
            {
                return true;
            }

            var rawSignature =
                $"accessKey={settings.AccessKey}" +
                $"&amount={response.Amount.ToString(CultureInfo.InvariantCulture)}" +
                $"&message={response.Message ?? string.Empty}" +
                $"&orderId={response.OrderId ?? string.Empty}" +
                $"&partnerCode={response.PartnerCode ?? string.Empty}" +
                $"&payUrl={response.PayUrl ?? string.Empty}" +
                $"&requestId={response.RequestId ?? string.Empty}" +
                $"&responseTime={response.ResponseTime.ToString(CultureInfo.InvariantCulture)}" +
                $"&resultCode={response.ResultCode.ToString(CultureInfo.InvariantCulture)}";

            return string.Equals(Sign(rawSignature), response.Signature, StringComparison.OrdinalIgnoreCase);
        }

        private string Sign(string rawSignature)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(settings.SecretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawSignature));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public class MomoCreatePaymentRequest
        {
            public string RequestId { get; set; } = string.Empty;

            public long Amount { get; set; }

            public string OrderId { get; set; } = string.Empty;

            public string OrderInfo { get; set; } = string.Empty;

            public string RedirectUrl { get; set; } = string.Empty;

            public string IpnUrl { get; set; } = string.Empty;

            public string ExtraData { get; set; } = string.Empty;

            public MomoUserInfo? UserInfo { get; set; }

            public List<MomoItemInfo>? Items { get; set; }
        }

        public class MomoCreatePaymentResult
        {
            public int ResultCode { get; set; }

            public string Message { get; set; } = string.Empty;

            public string PayUrl { get; set; } = string.Empty;

            public string DeepLink { get; set; } = string.Empty;

            public string QrCodeUrl { get; set; } = string.Empty;

            public string ErrorMessage { get; set; } = string.Empty;

            public string RawResponse { get; set; } = string.Empty;

            public bool IsSuccess =>
                ResultCode == 0 &&
                !string.IsNullOrWhiteSpace(PayUrl) &&
                string.IsNullOrWhiteSpace(ErrorMessage);
        }

        public class MomoPaymentCallbackPayload
        {
            public string PartnerCode { get; set; } = string.Empty;

            public string OrderId { get; set; } = string.Empty;

            public string RequestId { get; set; } = string.Empty;

            public long Amount { get; set; }

            public string OrderInfo { get; set; } = string.Empty;

            public string OrderType { get; set; } = string.Empty;

            public long TransId { get; set; }

            public int ResultCode { get; set; }

            public string Message { get; set; } = string.Empty;

            public string PayType { get; set; } = string.Empty;

            public long ResponseTime { get; set; }

            public string ExtraData { get; set; } = string.Empty;

            public string Signature { get; set; } = string.Empty;
        }

        public class MomoUserInfo
        {
            public string Name { get; set; } = string.Empty;

            public string PhoneNumber { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;
        }

        public class MomoItemInfo
        {
            public string Id { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;

            public long Price { get; set; }

            public int Quantity { get; set; }

            public long TotalPrice { get; set; }

            public string Currency { get; set; } = "VND";
        }

        private class MomoCreatePaymentPayload
        {
            public string PartnerCode { get; set; } = string.Empty;

            public string StoreId { get; set; } = string.Empty;

            public string StoreName { get; set; } = string.Empty;

            public string RequestId { get; set; } = string.Empty;

            public long Amount { get; set; }

            public string OrderId { get; set; } = string.Empty;

            public string OrderInfo { get; set; } = string.Empty;

            public string RedirectUrl { get; set; } = string.Empty;

            public string IpnUrl { get; set; } = string.Empty;

            public string RequestType { get; set; } = string.Empty;

            public string Lang { get; set; } = "vi";

            public bool AutoCapture { get; set; } = true;

            public string ExtraData { get; set; } = string.Empty;

            public string Signature { get; set; } = string.Empty;

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public MomoUserInfo? UserInfo { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public List<MomoItemInfo>? Items { get; set; }
        }

        private class MomoCreatePaymentResponse
        {
            public string PartnerCode { get; set; } = string.Empty;

            public string OrderId { get; set; } = string.Empty;

            public string RequestId { get; set; } = string.Empty;

            public long Amount { get; set; }

            public long ResponseTime { get; set; }

            public string Message { get; set; } = string.Empty;

            public int ResultCode { get; set; }

            public string PayUrl { get; set; } = string.Empty;

            public string Deeplink { get; set; } = string.Empty;

            public string QrCodeUrl { get; set; } = string.Empty;

            public string Signature { get; set; } = string.Empty;
        }
    }
}

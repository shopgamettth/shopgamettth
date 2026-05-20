using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Coffee.Data;
using Coffee.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace Coffee.Controllers
{
    [Authorize]
    public class CardChargeController : Controller
    {
        private readonly CoffeeShopDbContext _db;
        private readonly ILogger<CardChargeController> _logger;
        
        // Cấu hình TSR API
        private const string PartnerId = "32027401280";
        private const string PartnerKey = "ca31c019d27be9e6388f9f2e60e7dfd2";
        private const string ApiUrl = "https://thesieure.com/chargingws/v2";

        public CardChargeController(CoffeeShopDbContext db, ILogger<CardChargeController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitCard(string telco, string amount, string serial, string code)
        {
            if (string.IsNullOrEmpty(telco) || string.IsNullOrEmpty(amount) || 
                string.IsNullOrEmpty(serial) || string.IsNullOrEmpty(code))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin thẻ.";
                return RedirectToAction("Tsr", "Home");
            }

            var userIdStr = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var declaredValue = decimal.Parse(amount);
            var requestId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15) + DateTime.Now.Ticks.ToString().Substring(0, 5);

            // Sinh chữ ký: md5(partner_key + code + serial)
            var signString = PartnerKey + code + serial;
            var sign = CreateMD5(signString);

            var requestData = new
            {
                telco = telco,
                code = code,
                serial = serial,
                amount = int.Parse(amount),
                request_id = requestId,
                partner_id = PartnerId,
                sign = sign,
                command = "charging"
            };

            // Lưu thẻ vào DB trước khi gọi
            var cardCharge = new CardCharge
            {
                UserId = userId,
                Telco = telco,
                Code = code,
                Serial = serial,
                DeclaredValue = declaredValue,
                RequestId = requestId,
                Status = 99, // Chờ duyệt
                Message = "Đang gửi lên hệ thống TheSieuRe"
            };
            _db.CardCharges.Add(cardCharge);
            await _db.SaveChangesAsync();

            // Gửi API lên TSR
            using (var client = new HttpClient())
            {
                try
                {
                    var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(ApiUrl, content);
                    var resultStr = await response.Content.ReadAsStringAsync();
                    
                    var result = Newtonsoft.Json.Linq.JObject.Parse(resultStr);
                    int status = result["status"] != null ? (int)result["status"] : 100;
                    string message = result["message"] != null ? (string)result["message"] : "Không thể kết nối API";

                    cardCharge.Message = message;
                    cardCharge.Status = status;
                    await _db.SaveChangesAsync();

                    if (status == 99)
                    {
                        TempData["Success"] = "Gửi thẻ thành công. Hệ thống đang duyệt thẻ, vui lòng xem lịch sử!";
                    }
                    else
                    {
                        TempData["Error"] = $"Lỗi gửi thẻ: {message}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Lỗi gọi API TSR: " + ex.Message);
                    cardCharge.Status = 100;
                    cardCharge.Message = "Lỗi kết nối máy chủ";
                    await _db.SaveChangesAsync();
                    TempData["Error"] = "Đã xảy ra lỗi khi kết nối TheSieuRe. Vui lòng thử lại sau.";
                }
            }

            return RedirectToAction("Index", "Transactions");
        }

        private string CreateMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Coffee.Data;
using System.Linq;
using Coffee.Models;

namespace Coffee.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class TsrCallbackController : ControllerBase
    {
        private readonly CoffeeShopDbContext _db;
        private readonly ILogger<TsrCallbackController> _logger;
        
        private const string PartnerKey = "ca31c019d27be9e6388f9f2e60e7dfd2";

        public TsrCallbackController(CoffeeShopDbContext db, ILogger<TsrCallbackController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Callback([FromForm] TsrCallbackRequest req, [FromQuery] TsrCallbackRequest reqQuery)
        {
            // Hỗ trợ cả GET và POST từ TSR
            var data = req.status != 0 ? req : reqQuery;

            if (data == null || string.IsNullOrEmpty(data.request_id))
            {
                return BadRequest(new { status = false, message = "Thiếu dữ liệu" });
            }

            _logger.LogInformation($"TSR Callback: {data.request_id} - Status: {data.status} - Msg: {data.message}");

            var charge = _db.CardCharges.FirstOrDefault(x => x.RequestId == data.request_id);
            if (charge == null)
            {
                return Ok(new { status = false, message = "Không tìm thấy giao dịch" });
            }

            if (charge.Status == 1) // Đã cộng tiền rồi
            {
                return Ok(new { status = true, message = "Đã xử lý trước đó" });
            }

            // Kiểm tra chữ ký bảo mật: callback_sign = md5(partner_key + code + serial)
            var expectedSign = CreateMD5(PartnerKey + data.code + data.serial);
            if (expectedSign != data.callback_sign)
            {
                _logger.LogWarning($"Sai chữ ký callback TSR: {data.request_id}");
                return Ok(new { status = false, message = "Sai chữ ký bảo mật" });
            }

            charge.Status = data.status;
            charge.Message = data.message;
            charge.TransId = data.trans_id.ToString();
            charge.RealValue = data.value;
            charge.UpdatedAt = DateTime.UtcNow;

            // Nếu thành công đúng mệnh giá (status = 1) hoặc sai mệnh giá (status = 2) thì cộng tiền
            if (data.status == 1 || data.status == 2)
            {
                // Quy tắc Shop: Cộng 100% mệnh giá khách khai báo, hoặc mệnh giá thực
                // Ở đây mình ưu tiên cộng mệnh giá khai báo (declared_value) hoặc mệnh giá thực của thẻ (value).
                // Chọn cộng theo mệnh giá được duyệt thực tế (data.value) để đảm bảo không bị lỗi nếu khách khai gian
                var user = _db.Users.FirstOrDefault(u => u.UserId == charge.UserId);
                if (user != null && data.value > 0)
                {
                    user.Balance = (user.Balance ?? 0) + data.value;

                    // Thêm vào bảng Transaction lịch sử
                    var trans = new Transaction
                    {
                        UserId = user.UserId,
                        Amount = data.value,
                        Note = $"Nạp tự động TSR thẻ {charge.Telco} {data.value:N0}đ (Mã: {charge.RequestId})",
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Transactions.Add(trans);
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new { status = true, message = "Thành công" });
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

    public class TsrCallbackRequest
    {
        public int status { get; set; }
        public string message { get; set; }
        public string request_id { get; set; }
        public string trans_id { get; set; }
        public decimal declared_value { get; set; }
        public decimal value { get; set; }
        public decimal amount { get; set; }
        public string code { get; set; }
        public string serial { get; set; }
        public string telco { get; set; }
        public string callback_sign { get; set; }
    }
}

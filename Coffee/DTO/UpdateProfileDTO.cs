using System.ComponentModel.DataAnnotations;

namespace Coffee.DTO
{
    public class UpdateProfileDTO
    {
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "So dien thoai khong duoc de trong.")]
        [RegularExpression(@"^[0-9]{9,11}$", ErrorMessage = "So dien thoai phai tu 9 den 11 so.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Dia chi khong duoc de trong.")]
        [StringLength(300, ErrorMessage = "Dia chi toi da 300 ky tu.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã ID nạp tiền không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã ID tối đa 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Mã ID chỉ được chứa chữ cái và số.")]
        public string TransferCode { get; set; } = string.Empty;
    }
}

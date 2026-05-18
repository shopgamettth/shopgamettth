using System.ComponentModel.DataAnnotations;

namespace Coffee.DTO
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "Username không được để trống")]
        [StringLength(10, MinimumLength = 3, ErrorMessage = "Username từ 3 - 10 ký tự")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Chỉ chấp nhận đuôi là gmail.com")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Tối thiểu 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhập lại mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginDTO
    {
        //[Required(ErrorMessage = "Username không được để trống")]
        //public string Username { get; set; } = string.Empty;
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Chỉ chấp nhận đuôi là gmail.com")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordDTO
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Chỉ chấp nhận đuôi là gmail.com")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDTO
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Chỉ chấp nhận đuôi là gmail.com")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã xác nhận không được để trống")]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Mã xác nhận phải gồm đúng 6 chữ số")]
        public string VerificationCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [MinLength(6, ErrorMessage = "Tối thiểu 6 ký tự")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhập lại mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

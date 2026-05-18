using Coffee.Data;
using Coffee.DTO;
using Coffee.Helper;
using Coffee.Models;
using Coffee.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace Coffee.Controllers
{
    public class AuthController : Controller
    {
        private readonly CoffeeShopDbContext db;
        private readonly EmailService emailService;
        private readonly PasswordHasherHelper hasher = new PasswordHasherHelper();

        public AuthController(CoffeeShopDbContext context, EmailService emailService)
        {
            db = context;
            this.emailService = emailService;
        }

        // =========================
        // 📝 REGISTER
        // =========================

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(RegisterDTO dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                // check email
                if (db.Users.Any(x => x.Email == dto.Email))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại!");
                    return View(dto);
                }

                // check username
                if (db.Users.Any(x => x.UserName == dto.Username))
                {
                    ModelState.AddModelError("Username", "Username đã tồn tại!");
                    return View(dto);
                }

                var user = new User
                {
                    UserName = dto.Username,
                    Email = dto.Email,
                    RoleId = 2,
                    IsActive = true,
                    IsLocked = false,
                    CreatedAt = AppTimeHelper.UtcNow
                };
                
                // hash password
                user.Password = hasher.Hash(user, dto.Password);

                db.Users.Add(user);
                db.SaveChanges();

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                // 🔥 QUAN TRỌNG: show lỗi thật trên Render
                return Content($"ERROR REGISTER: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // =========================
        // 🔑 LOGIN (COOKIE)
        // =========================
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var user = db.Users.AsNoTracking().FirstOrDefault(x => x.Email == dto.Email);

            if (user == null || user.Password == null || !hasher.Verify(user, user.Password, dto.Password))
            {
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu!");
                return View(dto);
            }

            if (user.IsLocked == true)
            {
                ModelState.AddModelError("", "Tài khoản bị khóa!");
                return View(dto);
            }

            if (user.IsActive == false)
            {
                ModelState.AddModelError("", "Tài khoản chưa kích hoạt!");
                return View(dto);
            }

            // =========================
            // 🔥 CLAIMS (QUAN TRỌNG)
            // =========================
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.RoleId == 1 ? "Admin" : "User")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            // =========================
            // 🍪 SIGN IN
            // =========================
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true, // lưu cookie lâu dài (đến khi user logout hoặc cookie hết hạn)
                    ExpiresUtc = AppTimeHelper.UtcNow.AddMinutes(10) // ⏱ 10 phút
                }
            );

            return RedirectToAction("Index", "Products");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordDTO());
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var normalizedEmail = NormalizeEmail(dto.Email);
            var user = db.Users.FirstOrDefault(x => x.Email.ToLower() == normalizedEmail);

            try
            {
                if (user != null && user.IsLocked != true && user.IsActive != false)
                {
                    var verificationCode = GenerateResetCode();

                    user.PasswordResetCodeHash = HashResetCode(verificationCode);
                    user.PasswordResetCodeExpiresAt = AppTimeHelper.UtcNow.AddMinutes(10);

                    db.SaveChanges();

                    await emailService.SendPasswordResetCodeAsync(user.Email, user.UserName, verificationCode);
                }

                TempData["ResetPasswordInfo"] = "Neu email ton tai trong he thong, ma xac nhan da duoc gui ve Gmail cua ban.";
                return RedirectToAction(nameof(ResetPassword), new { email = dto.Email.Trim() });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Khong the gui ma xac nhan luc nay. {ex.Message}");
                return View(dto);
            }
        }

        [HttpGet]
        public IActionResult ResetPassword(string? email = null)
        {
            return View(new ResetPasswordDTO
            {
                Email = email ?? string.Empty
            });
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordDTO dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var normalizedEmail = NormalizeEmail(dto.Email);
            var user = db.Users.FirstOrDefault(x => x.Email.ToLower() == normalizedEmail);

            if (user == null ||
                string.IsNullOrWhiteSpace(user.PasswordResetCodeHash) ||
                user.PasswordResetCodeExpiresAt == null ||
                user.PasswordResetCodeExpiresAt < AppTimeHelper.UtcNow ||
                user.PasswordResetCodeHash != HashResetCode(dto.VerificationCode.Trim()))
            {
                ModelState.AddModelError(string.Empty, "Ma xac nhan khong hop le hoac da het han.");
                return View(dto);
            }

            if (hasher.Verify(user, user.Password, dto.NewPassword))
            {
                ModelState.AddModelError("NewPassword", "Mat khau moi khong duoc trung voi mat khau cu.");
                return View(dto);
            }

            user.Password = hasher.Hash(user, dto.NewPassword);
            user.PasswordResetCodeHash = null;
            user.PasswordResetCodeExpiresAt = null;

            db.SaveChanges();

            TempData["AuthSuccess"] = "Dat lai mat khau thanh cong. Bay gio ban co the dang nhap bang mat khau moi.";
            return RedirectToAction(nameof(Login));
        }

        // đổi mật khẩu (chỉ user đã login mới vào được)
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }
        [Authorize]
        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordDTO dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                // 🔥 lấy user hiện tại từ cookie
                var userId = User.FindFirst("UserId")?.Value;

                if (userId == null)
                    return RedirectToAction("Login");

                var user = db.Users.FirstOrDefault(x => x.UserId.ToString() == userId);

                if (user == null)
                    return RedirectToAction("Login");

                // 🔥 kiểm tra mật khẩu cũ
                if (!hasher.Verify(user, user.Password, dto.OldPassword))
                {
                    ModelState.AddModelError("OldPassword", "Mật khẩu cũ không đúng!");
                    return View(dto);
                }

                // 🔥 2. Check mật khẩu mới KHÔNG trùng
                if (hasher.Verify(user, user.Password, dto.NewPassword))
                {
                    ModelState.AddModelError("NewPassword", "Mật khẩu mới không được trùng mật khẩu cũ!");
                    return View(dto);
                }

                // 🔥 hash mật khẩu mới
                user.Password = hasher.Hash(user, dto.NewPassword);

                db.SaveChanges();

                ViewBag.Success = "Đổi mật khẩu thành công!";
                return View();
            }
            catch (Exception ex)
            {
                return Content($"ERROR CHANGE PASSWORD: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult Profile()
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (userId == null)
                return RedirectToAction("Login");

            var user = db.Users.FirstOrDefault(x => x.UserId.ToString() == userId);

            if (user == null)
                return RedirectToAction("Login");

            return View(new UpdateProfileDTO
            {
                Username = user.UserName,
                Email = user.Email,
                Phone = user.Phone ?? string.Empty,
                Address = user.Address ?? string.Empty
            });
        }

        [Authorize]
        [HttpPost]
        public IActionResult Profile(UpdateProfileDTO dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                var userId = User.FindFirst("UserId")?.Value;

                if (userId == null)
                    return RedirectToAction("Login");

                var user = db.Users.FirstOrDefault(x => x.UserId.ToString() == userId);

                if (user == null)
                    return RedirectToAction("Login");

                user.Phone = dto.Phone.Trim();
                user.Address = dto.Address.Trim();

                db.SaveChanges();

                ViewBag.Success = "Cap nhat thong tin nhan hang thanh cong!";

                dto.Username = user.UserName;
                dto.Email = user.Email;

                return View(dto);
            }
            catch (Exception ex)
            {
                return Content($"ERROR UPDATE PROFILE: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // =========================
        // 🚪 LOGOUT
        // =========================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private static string NormalizeEmail(string email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string GenerateResetCode()
        {
            return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        }

        private static string HashResetCode(string code)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim())));
        }
    }
}

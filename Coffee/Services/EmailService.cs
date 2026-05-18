using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Coffee.Services
{
    public class EmailService
    {
        private readonly EmailSettings settings;

        public EmailService(IOptions<EmailSettings> options)
        {
            settings = options.Value;
        }

        public async Task SendPasswordResetCodeAsync(string toEmail, string recipientName, string verificationCode)
        {
            EnsureConfigured();

            using var message = new MailMessage
            {
                From = new MailAddress(settings.SenderEmail, settings.SenderName),
                Subject = "Coffee Shop - Ma xac nhan dat lai mat khau",
                IsBodyHtml = true,
                Body = $@"
<div style='font-family:Arial,sans-serif;line-height:1.6;color:#2d1f19'>
    <h2 style='margin-bottom:8px'>Xin chao {WebUtility.HtmlEncode(recipientName)},</h2>
    <p>Ban vua yeu cau dat lai mat khau cho tai khoan Coffee Shop.</p>
    <p>Ma xac nhan cua ban la:</p>
    <div style='display:inline-block;padding:14px 24px;margin:8px 0;background:#35211d;color:#fff;font-size:28px;font-weight:700;letter-spacing:6px;border-radius:12px'>
        {WebUtility.HtmlEncode(verificationCode)}
    </div>
    <p>Ma nay co hieu luc trong <strong>10 phut</strong>.</p>
    <p>Neu ban khong yeu cau dat lai mat khau, hay bo qua email nay.</p>
</div>"
            };

            message.To.Add(new MailAddress(toEmail));

            using var smtp = new SmtpClient(settings.SmtpHost, settings.Port)
            {
                Credentials = new NetworkCredential(settings.Username, settings.Password),
                EnableSsl = settings.EnableSsl
            };

            await smtp.SendMailAsync(message);
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(settings.SmtpHost) ||
                IsPlaceholder(settings.SenderEmail, "your-gmail@gmail.com") ||
                IsPlaceholder(settings.Username, "your-gmail@gmail.com") ||
                IsPlaceholder(settings.Password, "your-gmail-app-password"))
            {
                throw new InvalidOperationException(
                    "EmailSettings chua duoc cau hinh Gmail that. Hay cap nhat SenderEmail, Username, Password bang Gmail va App Password that, hoac dat qua bien moi truong EmailSettings__SenderEmail, EmailSettings__Username, EmailSettings__Password.");
            }
        }

        private static bool IsPlaceholder(string value, string placeholder)
        {
            return string.IsNullOrWhiteSpace(value) ||
                   value.Contains(placeholder, StringComparison.OrdinalIgnoreCase);
        }
    }
}

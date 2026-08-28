using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.Identity;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace WebHoanTien.Web.Pages.Account;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    private const int TemporaryPasswordLength = 16;
    private const string UpperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijkmnopqrstuvwxyz";
    private const string DigitChars = "23456789";
    private const string SymbolChars = "!@$?_#-";
    private const string GenericStatusMessage =
        "Nếu email khớp với email đăng nhập hoặc email liên hệ của tài khoản đăng ký trực tiếp trên CatBack, mật khẩu mới sẽ được gửi tới email bạn vừa nhập. Vui lòng kiểm tra cả Hộp thư đến và Spam.";

    private readonly IdentityUserManager _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ForgotPasswordModel> _logger;

    [BindProperty]
    [Required(ErrorMessage = "Vui lòng nhập email nhận mật khẩu.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;

    public string? StatusMessage { get; private set; }

    public ForgotPasswordModel(
        IdentityUserManager userManager,
        IConfiguration configuration,
        ILogger<ForgotPasswordModel> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var recipient = Email.Trim();

        // UserName is the login email. IdentityUser.Email is the editable contact email.
        // Both can be used to request a password reset, but they must resolve to one user.
        var userByLoginEmail = await _userManager.FindByNameAsync(recipient);
        var userByContactEmail = await _userManager.FindByEmailAsync(recipient);

        if (userByLoginEmail is not null &&
            userByContactEmail is not null &&
            userByLoginEmail.Id != userByContactEmail.Id)
        {
            _logger.LogWarning(
                "Forgot-password request is ambiguous because recipient email matches different users.");
            StatusMessage = GenericStatusMessage;
            return Page();
        }

        var user = userByLoginEmail ?? userByContactEmail;

        // Do not reveal whether an account exists or whether it is Google-only.
        if (user is null || !await _userManager.HasPasswordAsync(user))
        {
            StatusMessage = GenericStatusMessage;
            return Page();
        }

        if (!IsValidEmail(recipient))
        {
            _logger.LogWarning(
                "Forgot-password request cannot send mail because recipient is not a valid email for user {UserId}.",
                user.Id);
            StatusMessage = GenericStatusMessage;
            return Page();
        }

        try
        {
            var temporaryPassword = await GenerateValidTemporaryPasswordAsync(user);
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Send first. If SMTP fails, the existing password remains unchanged and the
            // user is not locked out by a password they never received.
            await SendTemporaryPasswordAsync(recipient, temporaryPassword);

            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, temporaryPassword);
            if (!resetResult.Succeeded)
            {
                _logger.LogError(
                    "Password reset failed after email delivery for user {UserId}. Error codes: {ErrorCodes}",
                    user.Id,
                    string.Join(", ", resetResult.Errors.Select(x => x.Code)));
            }
        }
        catch (Exception exception)
        {
            // Keep the public response generic to avoid account enumeration and SMTP detail leakage.
            _logger.LogError(exception, "Forgot-password processing failed.");
        }

        StatusMessage = GenericStatusMessage;
        return Page();
    }

    private async Task<string> GenerateValidTemporaryPasswordAsync(IdentityUser user)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidate = GenerateTemporaryPassword();
            var isValid = true;

            foreach (var validator in _userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(_userManager, user, candidate);
                if (result.Succeeded)
                {
                    continue;
                }

                isValid = false;
                break;
            }

            if (isValid)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not generate a password that satisfies the configured password policy.");
    }

    private async Task SendTemporaryPasswordAsync(string recipient, string temporaryPassword)
    {
        var smtp = ReadSmtpConfiguration();

        using var message = new MailMessage
        {
            From = new MailAddress(smtp.FromAddress, smtp.FromName),
            Subject = "[CatBack] Mật khẩu đăng nhập mới",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false,
            Body = string.Join(Environment.NewLine, new[]
            {
                "Xin chào,",
                string.Empty,
                "CatBack đã nhận được yêu cầu quên mật khẩu cho tài khoản của bạn.",
                string.Empty,
                "Mật khẩu đăng nhập mới:",
                temporaryPassword,
                string.Empty,
                "Hãy đăng nhập bằng mật khẩu này và đổi mật khẩu ngay tại Tài khoản > Đổi mật khẩu.",
                string.Empty,
                "Nếu bạn không yêu cầu thao tác này, vui lòng đăng nhập và đổi mật khẩu hoặc liên hệ quản trị viên CatBack.",
                string.Empty,
                "CatBack"
            })
        };
        message.To.Add(new MailAddress(recipient));

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(smtp.UserName))
        {
            client.Credentials = new NetworkCredential(smtp.UserName, smtp.Password);
        }

        await client.SendMailAsync(message);
    }

    private SmtpConfiguration ReadSmtpConfiguration()
    {
        var host = ReadSetting("SMTP_HOST", "Smtp:Host");
        var userName = ReadSetting("SMTP_USERNAME", "Smtp:UserName");
        var password = ReadSetting("SMTP_PASSWORD", "Smtp:Password");
        var fromAddress = ReadSetting("SMTP_FROM_ADDRESS", "Smtp:FromAddress");
        var fromName = ReadSetting("SMTP_FROM_NAME", "Smtp:FromName");

        var portValue = ReadSetting("SMTP_PORT", "Smtp:Port");
        var sslValue = ReadSetting("SMTP_ENABLE_SSL", "Smtp:EnableSsl");

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("SMTP host is not configured.");
        }

        if (string.IsNullOrWhiteSpace(fromAddress) || !IsValidEmail(fromAddress))
        {
            throw new InvalidOperationException("SMTP from address is not configured or is invalid.");
        }

        var port = int.TryParse(portValue, out var parsedPort) ? parsedPort : 587;
        var enableSsl = !bool.TryParse(sslValue, out var parsedSsl) || parsedSsl;

        return new SmtpConfiguration(
            host,
            port,
            userName,
            password,
            fromAddress,
            string.IsNullOrWhiteSpace(fromName) ? "CatBack" : fromName,
            enableSsl);
    }

    private string ReadSetting(string environmentVariable, string configurationKey)
    {
        return Environment.GetEnvironmentVariable(environmentVariable)
               ?? _configuration[configurationKey]
               ?? string.Empty;
    }

    private static bool IsValidEmail(string value)
    {
        try
        {
            var address = new MailAddress(value);
            return string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string GenerateTemporaryPassword()
    {
        var characters = new List<char>(TemporaryPasswordLength)
        {
            PickRandom(UpperChars),
            PickRandom(LowerChars),
            PickRandom(DigitChars),
            PickRandom(SymbolChars)
        };

        var allChars = UpperChars + LowerChars + DigitChars + SymbolChars;
        while (characters.Count < TemporaryPasswordLength)
        {
            characters.Add(PickRandom(allChars));
        }

        for (var i = characters.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (characters[i], characters[j]) = (characters[j], characters[i]);
        }

        return new string(characters.ToArray());
    }

    private static char PickRandom(string source)
    {
        return source[RandomNumberGenerator.GetInt32(source.Length)];
    }

    private sealed record SmtpConfiguration(
        string Host,
        int Port,
        string UserName,
        string Password,
        string FromAddress,
        string FromName,
        bool EnableSsl);
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Web.Payments;

public sealed record VietQrResult(string TransferContent, string? ImageUrl, string? Error);

// Read-only presentation utility. No wallet or withdrawal service dependencies.
public sealed class VietQrService : ITransientDependency
{
    // VietQR bank directory, checked 2026-09-10: https://api.vietqr.io/v2/banks
    // CTG and AGR are the existing CatBack aliases for ICB and VBA.
    private static readonly IReadOnlyDictionary<string, string> BankBins =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["VCB"] = "970436", ["BIDV"] = "970418", ["CTG"] = "970415",
            ["AGR"] = "970405", ["TCB"] = "970407", ["MB"] = "970422",
            ["ACB"] = "970416", ["VPB"] = "970432", ["TPB"] = "970423",
            ["VIB"] = "970441", ["STB"] = "970403", ["HDB"] = "970437",
            ["MSB"] = "970426", ["SHB"] = "970443", ["OCB"] = "970448",
            ["EIB"] = "970431", ["LPB"] = "970449", ["NAB"] = "970428",
            ["SEAB"] = "970440", ["BAB"] = "970409"
        };

    private readonly IConfiguration _configuration;
    public VietQrService(IConfiguration configuration) => _configuration = configuration;

    public VietQrResult Generate(string? bankCode, string? accountNumber, decimal amount, string? requestCode)
    {
        var content = $"CB {requestCode?.Trim()}";
        if (!BankBins.TryGetValue(bankCode?.Trim() ?? "", out var bin) ||
            string.IsNullOrWhiteSpace(accountNumber))
            return new(content, null, "Không thể tạo QR do thiếu thông tin ngân hàng.");
        if (!Regex.IsMatch(accountNumber, "\\A[A-Za-z0-9]{1,19}\\z"))
            return new(content, null, "Số tài khoản không phù hợp để tạo VietQR. Vui lòng chuyển khoản thủ công.");
        if (amount <= 0 || amount != decimal.Truncate(amount) || amount > 9999999999999m)
            return new(content, null, "Không thể tạo QR do số tiền không hợp lệ.");
        if (string.IsNullOrWhiteSpace(requestCode) || !Regex.IsMatch(content, "\\A[A-Za-z0-9 ]{1,50}\\z"))
            return new(content, null, "Không thể tạo QR do nội dung chuyển khoản không hợp lệ.");

        var template = _configuration["VietQr:Template"] ?? "qr_only";
        if (!Regex.IsMatch(template, "\\A[A-Za-z0-9_-]{1,100}\\z"))
            return new(content, null, "Không thể tải mã QR. Bạn vẫn có thể chuyển khoản thủ công.");
        var url = $"https://img.vietqr.io/image/{bin}-{Uri.EscapeDataString(accountNumber)}-{template}.png" +
            $"?amount={amount.ToString("0", CultureInfo.InvariantCulture)}&addInfo={Uri.EscapeDataString(content)}";
        return new(content, url, null);
    }
}

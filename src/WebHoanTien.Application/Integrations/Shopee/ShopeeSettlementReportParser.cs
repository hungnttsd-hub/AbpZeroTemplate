using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Integrations.Shopee;

public sealed record ShopeeSettlementRow(string ExternalOrderId, decimal ActualPaidCommission,
    string? PaymentReference, DateTime? PaidAt);

public sealed record ShopeeSettlementParseResult(int RowCount, IReadOnlyList<string> Headers,
    IReadOnlyList<ShopeeSettlementRow> Rows);

public class ShopeeSettlementReportParser : ITransientDependency
{
    public async Task<ShopeeSettlementParseResult> ParseAsync(Stream stream,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content)) throw InvalidReport("Bảng kê thanh toán đang trống.");

        var records = ParseCsv(content, DetectDelimiter(content));
        if (records.Count < 2) throw InvalidReport("Bảng kê phải có header và ít nhất một dòng dữ liệu.");

        var headers = records[0].Select(value => value.Trim()).ToList();
        var columns = headers.Select((value, index) => new { Key = Normalize(value), Index = index })
            .Where(value => !string.IsNullOrWhiteSpace(value.Key))
            .GroupBy(value => value.Key, StringComparer.Ordinal)
            .ToDictionary(value => value.Key, value => value.First().Index, StringComparer.Ordinal);
        var orderIdColumn = RequiredColumn(columns, "ID đơn hàng", "orderid", "orderno", "iddonhang",
            "madonhang", "madon", "ordercode");
        var paidAmountColumn = RequiredColumn(columns, "Số tiền Shopee thực trả", "actualpaidcommission",
            "paidcommission", "netpayment", "actualpayment", "sotienthucnhan", "sotienthanhtoan",
            "sotienshopeethuctra", "hoahongthuctra", "hoahongdathanhtoan", "hoahongthanhtoan",
            "hoahongsauthue");
        var referenceColumn = OptionalColumn(columns, "paymentreference", "statementid", "invoiceid",
            "mathanhtoan", "mabangke", "mahoadon");
        var paidAtColumn = OptionalColumn(columns, "paidat", "paymentdate", "ngaythanhtoan",
            "thoigianthanhtoan", "ngaychuyentien");

        var rows = new List<ShopeeSettlementRow>();
        for (var index = 1; index < records.Count; index++)
        {
            var record = records[index];
            if (record.All(string.IsNullOrWhiteSpace)) continue;
            var rowNumber = index + 1;
            var orderId = GetValue(record, orderIdColumn);
            if (string.IsNullOrWhiteSpace(orderId)) throw InvalidReport($"Dòng {rowNumber}: thiếu ID đơn hàng.");
            rows.Add(new ShopeeSettlementRow(orderId,
                ParseRequiredDecimal(GetValue(record, paidAmountColumn), rowNumber),
                NullIfWhiteSpace(GetValue(record, referenceColumn)),
                ParseOptionalDate(GetValue(record, paidAtColumn), rowNumber)));
        }

        if (rows.Count == 0) throw InvalidReport("Bảng kê không có dòng dữ liệu hợp lệ.");
        var groupedRows = rows.GroupBy(row => row.ExternalOrderId, StringComparer.Ordinal)
            .Select(group => new ShopeeSettlementRow(group.Key, group.Sum(row => row.ActualPaidCommission),
                group.Select(row => row.PaymentReference).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
                group.Max(row => row.PaidAt)))
            .ToList();
        return new ShopeeSettlementParseResult(rows.Count, headers, groupedRows);
    }

    private static decimal ParseRequiredDecimal(string value, int rowNumber)
    {
        if (string.IsNullOrWhiteSpace(value)) throw InvalidReport($"Dòng {rowNumber}: thiếu số tiền Shopee thực trả.");
        var sanitized = Regex.Replace(value, @"[^0-9,.-]", string.Empty);
        var lastComma = sanitized.LastIndexOf(',');
        var lastDot = sanitized.LastIndexOf('.');
        if (lastComma >= 0 && lastDot >= 0)
            sanitized = lastComma > lastDot ? sanitized.Replace(".", string.Empty).Replace(',', '.') : sanitized.Replace(",", string.Empty);
        else if (lastComma >= 0)
            sanitized = sanitized[(lastComma + 1)..].Length <= 2 ? sanitized.Replace(',', '.') : sanitized.Replace(",", string.Empty);
        else if (lastDot >= 0 && sanitized.Count(character => character == '.') > 1)
            sanitized = sanitized.Replace(".", string.Empty);
        if (!decimal.TryParse(sanitized, NumberStyles.Number | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out var result) || result < 0m)
            throw InvalidReport($"Dòng {rowNumber}: số tiền Shopee thực trả không hợp lệ.");
        return result;
    }

    private static DateTime? ParseOptionalDate(string value, int rowNumber)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTimeOffset.TryParse(value, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.AllowWhiteSpaces,
                out var offset)) return offset.UtcDateTime;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date))
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        throw InvalidReport($"Dòng {rowNumber}: ngày thanh toán không hợp lệ.");
    }

    private static int RequiredColumn(IReadOnlyDictionary<string, int> columns, string displayName,
        params string[] aliases) => OptionalColumn(columns, aliases) ??
        throw InvalidReport($"Không thấy cột bắt buộc: {displayName}.");

    private static int? OptionalColumn(IReadOnlyDictionary<string, int> columns, params string[] aliases)
    {
        foreach (var alias in aliases)
            if (columns.TryGetValue(Normalize(alias), out var index)) return index;
        return null;
    }

    private static string GetValue(IReadOnlyList<string> record, int? column) =>
        column.HasValue && column.Value < record.Count ? record[column.Value].Trim() : string.Empty;

    private static string? NullIfWhiteSpace(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static char DetectDelimiter(string content)
    {
        var header = content.Split(new[] { "\r\n", "\n" }, 2, StringSplitOptions.None)[0];
        return new[] { ',', ';', '\t' }.OrderByDescending(delimiter => header.Count(character => character == delimiter)).First();
    }

    private static List<List<string>> ParseCsv(string content, char delimiter)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < content.Length; index++)
        {
            var character = content[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < content.Length && content[index + 1] == '"')
                {
                    value.Append(character);
                    index++;
                }
                else if (character == '"') quoted = false;
                else value.Append(character);
                continue;
            }

            if (character == '"' && value.Length == 0) quoted = true;
            else if (character == delimiter) { record.Add(value.ToString()); value.Clear(); }
            else if (character == '\n') { record.Add(value.ToString()); records.Add(record); record = new(); value.Clear(); }
            else if (character != '\r') value.Append(character);
        }
        if (quoted) throw InvalidReport("CSV có dấu nháy chưa được đóng.");
        if (value.Length > 0 || record.Count > 0) { record.Add(value.ToString()); records.Add(record); }
        return records;
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (character is 'đ' or 'Đ') { builder.Append('d'); continue; }
            if (CharUnicodeInfo.GetUnicodeCategory(character) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpaceSeparator ||
                character is '_' or '-' or '.') continue;
            if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
        }
        return builder.ToString();
    }

    private static UserFriendlyException InvalidReport(string message) =>
        new(message, code: WebHoanTienDomainErrorCodes.InvalidShopeeSettlementReport);
}

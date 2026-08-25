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
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations;

namespace WebHoanTien.Integrations.Shopee;

public sealed record ShopeeReportParseResult(
    int RowCount,
    IReadOnlyList<string> Headers,
    IReadOnlyList<NormalizedAffiliateConversion> Conversions);

public class ShopeeReportParser : ITransientDependency
{
    private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

    public async Task<ShopeeReportParseResult> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content)) throw InvalidReport("File báo cáo đang trống.");

        var records = ParseCsv(content, DetectDelimiter(content));
        if (records.Count < 2) throw InvalidReport("Báo cáo phải có header và ít nhất một dòng dữ liệu.");

        var headers = records[0].Select(value => value.Trim()).ToList();
        var columns = headers.Select((value, index) => new { Key = Normalize(value), Index = index })
            .Where(value => !string.IsNullOrWhiteSpace(value.Key))
            .GroupBy(value => value.Key, StringComparer.Ordinal)
            .ToDictionary(value => value.Key, value => value.First().Index, StringComparer.Ordinal);

        var orderIdColumn = RequiredColumn(columns, "Mã đơn hàng", "orderid", "orderno", "iddonhang", "madonhang", "madon", "ordercode");
        var subIdColumns = OptionalColumns(columns, "subid", "subids", "sub1", "subid1", "subid2", "subid3", "subid4", "subid5",
            "utmcontent", "trackingid");
        if (subIdColumns.Count == 0) throw InvalidReport("Không thấy cột bắt buộc: Sub ID.");
        var purchaseTimeColumn = RequiredColumn(columns, "Thời gian đặt hàng", "purchasetime", "ordertime", "ordercreatedtime",
            "ngaydathang", "thoigiandathang", "thoigiantao", "createdtime");
        var commissionColumn = RequiredColumn(columns, "Hoa hồng thực nhận", "netcommission", "actualcommission", "commission",
            "hoahongthucnhan", "hoahongrongtiepthilienket", "hoahong", "commissionvalue");

        var conversionIdColumn = OptionalColumn(columns, "conversionid", "transactionid", "conversion", "ma giaodich", "magiaodich");
        var statusColumn = OptionalColumn(columns, "orderstatus", "status", "trangthaidathang", "trangthaidonhang", "trangthai");
        var purchaseAmountColumn = OptionalColumn(columns, "purchaseamount", "ordervalue", "actualamount", "gmv", "giatridonhang", "giatri");
        var itemIdColumn = OptionalColumn(columns, "itemid", "productid", "masanpham", "id san pham", "idsanpham");
        var modelIdColumn = OptionalColumn(columns, "modelid", "idmodel", "model", "variationid", "maphanloai");
        var productNameColumn = OptionalColumn(columns, "productname", "itemname", "tenitem", "tensanpham", "ten san pham");
        var quantityColumn = OptionalColumn(columns, "quantity", "qty", "soluong");
        var refundAmountColumn = OptionalColumn(columns, "refundamount", "refund", "sotienhoantra", "tienhoan", "hoantien");
        var fraudColumn = OptionalColumn(columns, "fraudstatus", "fraud", "gianlan");

        var rows = new List<ReportRow>();
        for (var recordIndex = 1; recordIndex < records.Count; recordIndex++)
        {
            var record = records[recordIndex];
            if (record.All(string.IsNullOrWhiteSpace)) continue;

            var rowNumber = recordIndex + 1;
            var orderId = GetValue(record, orderIdColumn);
            if (string.IsNullOrWhiteSpace(orderId)) throw InvalidReport($"Dòng {rowNumber}: thiếu Mã đơn hàng.");

            var rawSubIds = subIdColumns.Select(column => GetValue(record, column)).ToList();
            if (rawSubIds.All(string.IsNullOrWhiteSpace)) throw InvalidReport($"Dòng {rowNumber}: thiếu Sub ID.");
            var attributionValue = ResolveAttributionValue(rawSubIds);

            var purchaseTime = ParsePurchaseTime(GetValue(record, purchaseTimeColumn), rowNumber);
            var commission = ParseRequiredDecimal(GetValue(record, commissionColumn), "Hoa hồng thực nhận", rowNumber);
            var conversionId = GetValue(record, conversionIdColumn);
            rows.Add(new ReportRow(
                string.IsNullOrWhiteSpace(conversionId) ? orderId : conversionId,
                orderId,
                attributionValue,
                purchaseTime,
                ParseOrderStatus(GetValue(record, statusColumn)),
                GetValue(record, statusColumn),
                ParseDecimal(GetValue(record, purchaseAmountColumn)),
                commission,
                GetValue(record, itemIdColumn),
                GetValue(record, modelIdColumn),
                GetValue(record, productNameColumn),
                ParsePositiveInteger(GetValue(record, quantityColumn)),
                ParseDecimal(GetValue(record, refundAmountColumn)),
                IsFraud(GetValue(record, fraudColumn))));
        }

        if (rows.Count == 0) throw InvalidReport("Báo cáo không có dòng dữ liệu hợp lệ.");
        return new ShopeeReportParseResult(rows.Count, headers, rows.GroupBy(row => row.ConversionId, StringComparer.Ordinal)
            .Select(ToConversion).ToList());
    }

    private static NormalizedAffiliateConversion ToConversion(IGrouping<string, ReportRow> conversionRows)
    {
        var orders = conversionRows.GroupBy(row => row.OrderId, StringComparer.Ordinal).Select(orderRows =>
        {
            var rows = orderRows.ToList();
            var items = rows.GroupBy(row => new
                {
                    ExternalItemId = string.IsNullOrWhiteSpace(row.ItemId) ? "ORDER:" + orderRows.Key : row.ItemId,
                    ModelId = row.ModelId ?? string.Empty
                })
                .Select(itemRows =>
                {
                    var item = itemRows.ToList();
                    return new NormalizedAffiliateOrderItem(
                        itemRows.Key.ExternalItemId,
                        string.IsNullOrWhiteSpace(itemRows.Key.ModelId) ? null : itemRows.Key.ModelId,
                        item.Select(row => row.ProductName).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
                        item.Sum(row => row.PurchaseAmount),
                        item.Sum(row => row.Quantity),
                        item.Sum(row => row.NetCommission),
                        item.Sum(row => row.RefundAmount),
                        item.Any(row => row.IsFraud),
                        item.Select(row => row.ProviderStatus).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)));
                }).ToList();
            return new NormalizedAffiliateOrder(orderRows.Key, DeriveOrderStatus(rows), null,
                rows.Sum(row => row.PurchaseAmount), rows.Sum(row => row.NetCommission), items);
        }).ToList();

        var allRows = conversionRows.ToList();
        return new NormalizedAffiliateConversion(
            conversionRows.Key,
            allRows.Select(row => row.AttributionValue).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
            allRows.Min(row => row.PurchaseTime),
            null,
            DeriveConversionStatus(orders),
            allRows.Sum(row => row.NetCommission),
            allRows.Sum(row => row.NetCommission),
            CommissionSource.NetCommission,
            orders);
    }

    private static AffiliateOrderStatus DeriveOrderStatus(IReadOnlyCollection<ReportRow> rows)
    {
        if (rows.All(row => row.Status == AffiliateOrderStatus.Cancelled)) return AffiliateOrderStatus.Cancelled;
        if (rows.All(row => row.Status == AffiliateOrderStatus.Refunded)) return AffiliateOrderStatus.Refunded;
        if (rows.All(row => row.Status == AffiliateOrderStatus.Rejected)) return AffiliateOrderStatus.Rejected;
        return rows.All(row => row.Status == AffiliateOrderStatus.Completed)
            ? AffiliateOrderStatus.Completed
            : AffiliateOrderStatus.Pending;
    }

    private static AffiliateConversionStatus DeriveConversionStatus(IReadOnlyCollection<NormalizedAffiliateOrder> orders)
    {
        if (orders.All(order => order.Status == AffiliateOrderStatus.Cancelled)) return AffiliateConversionStatus.Cancelled;
        if (orders.All(order => order.Status == AffiliateOrderStatus.Refunded)) return AffiliateConversionStatus.Refunded;
        if (orders.All(order => order.Status == AffiliateOrderStatus.Rejected)) return AffiliateConversionStatus.Rejected;
        return orders.All(order => order.Status == AffiliateOrderStatus.Completed)
            ? AffiliateConversionStatus.Approved
            : AffiliateConversionStatus.Pending;
    }

    private static AffiliateOrderStatus ParseOrderStatus(string value)
    {
        var normalized = Normalize(value);
        if (string.IsNullOrWhiteSpace(normalized)) return AffiliateOrderStatus.Pending;
        if (normalized.Contains("refund", StringComparison.Ordinal) || normalized.Contains("hoantien", StringComparison.Ordinal)) return AffiliateOrderStatus.Refunded;
        if (normalized.Contains("cancel", StringComparison.Ordinal) || normalized.Contains("huy", StringComparison.Ordinal)) return AffiliateOrderStatus.Cancelled;
        if (normalized.Contains("reject", StringComparison.Ordinal) || normalized.Contains("tuchoi", StringComparison.Ordinal)) return AffiliateOrderStatus.Rejected;
        if (normalized.Contains("pending", StringComparison.Ordinal) || normalized.Contains("unpaid", StringComparison.Ordinal) ||
            normalized.Contains("choxuly", StringComparison.Ordinal) || normalized.Contains("chothanhtoan", StringComparison.Ordinal)) return AffiliateOrderStatus.Pending;
        if (normalized.Contains("complete", StringComparison.Ordinal) || normalized.Contains("hoanthanh", StringComparison.Ordinal) ||
            normalized.Contains("approved", StringComparison.Ordinal) || normalized.Contains("daxacnhan", StringComparison.Ordinal))
            return AffiliateOrderStatus.Completed;
        return AffiliateOrderStatus.Pending;
    }

    private static bool IsFraud(string value)
    {
        var normalized = Normalize(value);
        return normalized.Contains("fraud", StringComparison.Ordinal) || normalized.Contains("gianlan", StringComparison.Ordinal) ||
               normalized is "true" or "1" or "yes";
    }

    private static string? ExtractTrackingToken(string value)
    {
        var match = Regex.Match(value, @"[A-Za-z0-9_-]{24,64}");
        return match.Success ? match.Value : null;
    }

    private static string ResolveAttributionValue(IReadOnlyList<string> subIds)
    {
        var populatedSubIds = subIds.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
        if (populatedSubIds.Count > 1)
        {
            var reconstructedToken = string.Join("-", populatedSubIds);
            var extractedToken = ExtractTrackingToken(reconstructedToken);
            if (!string.IsNullOrWhiteSpace(extractedToken)) return extractedToken;
        }

        return populatedSubIds.Select(ExtractTrackingToken).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ??
               populatedSubIds.First();
    }

    private static DateTime ParsePurchaseTime(string value, int rowNumber)
    {
        if (string.IsNullOrWhiteSpace(value)) throw InvalidReport($"Dòng {rowNumber}: thiếu Thời gian đặt hàng.");
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var offset))
        {
            return offset.UtcDateTime;
        }

        var cultures = new[] { CultureInfo.GetCultureInfo("vi-VN"), CultureInfo.InvariantCulture };
        foreach (var culture in cultures)
        {
            if (DateTime.TryParse(value, culture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
            {
                var localTime = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
                return new DateTimeOffset(localTime, VietnamTimeZone.GetUtcOffset(localTime)).UtcDateTime;
            }
        }

        throw InvalidReport($"Dòng {rowNumber}: Thời gian đặt hàng không hợp lệ.");
    }

    private static decimal ParseRequiredDecimal(string value, string columnName, int rowNumber)
    {
        if (string.IsNullOrWhiteSpace(value)) throw InvalidReport($"Dòng {rowNumber}: thiếu {columnName}.");
        return ParseDecimal(value, columnName, rowNumber);
    }

    private static decimal ParseDecimal(string value, string? columnName = null, int? rowNumber = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0m;
        var sanitized = Regex.Replace(value, @"[^0-9,.-]", string.Empty);
        if (string.IsNullOrWhiteSpace(sanitized) || sanitized is "-" or "." or ",")
        {
            throw InvalidNumber(columnName, rowNumber);
        }

        var lastComma = sanitized.LastIndexOf(',');
        var lastDot = sanitized.LastIndexOf('.');
        if (lastComma >= 0 && lastDot >= 0)
        {
            sanitized = lastComma > lastDot
                ? sanitized.Replace(".", string.Empty).Replace(',', '.')
                : sanitized.Replace(",", string.Empty);
        }
        else if (lastComma >= 0)
        {
            sanitized = sanitized[(lastComma + 1)..].Length <= 2
                ? sanitized.Replace(',', '.')
                : sanitized.Replace(",", string.Empty);
        }
        else if (lastDot >= 0 && sanitized.Count(character => character == '.') > 1)
        {
            sanitized = sanitized.Replace(".", string.Empty);
        }

        return decimal.TryParse(sanitized, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw InvalidNumber(columnName, rowNumber);
    }

    private static int ParsePositiveInteger(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 1;
        var number = ParseDecimal(value);
        return number > 0m && number <= int.MaxValue ? decimal.ToInt32(decimal.Truncate(number)) : 1;
    }

    private static int RequiredColumn(IReadOnlyDictionary<string, int> columns, string displayName, params string[] aliases)
    {
        var result = OptionalColumn(columns, aliases);
        return result ?? throw InvalidReport($"Không thấy cột bắt buộc: {displayName}.");
    }

    private static int? OptionalColumn(IReadOnlyDictionary<string, int> columns, params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (columns.TryGetValue(Normalize(alias), out var index)) return index;
        }

        return null;
    }

    private static IReadOnlyList<int> OptionalColumns(IReadOnlyDictionary<string, int> columns, params string[] aliases) =>
        aliases.Select(alias => OptionalColumn(columns, alias)).Where(index => index.HasValue).Select(index => index!.Value).Distinct().ToList();

    private static string GetValue(IReadOnlyList<string> record, int? column) =>
        column.HasValue && column.Value < record.Count ? record[column.Value].Trim() : string.Empty;

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
                if (character == '"')
                {
                    if (index + 1 < content.Length && content[index + 1] == '"')
                    {
                        value.Append(character);
                        index++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    value.Append(character);
                }

                continue;
            }

            if (character == '"' && value.Length == 0)
            {
                quoted = true;
            }
            else if (character == delimiter)
            {
                record.Add(value.ToString());
                value.Clear();
            }
            else if (character == '\n')
            {
                record.Add(value.ToString());
                records.Add(record);
                record = new List<string>();
                value.Clear();
            }
            else if (character != '\r')
            {
                value.Append(character);
            }
        }

        if (quoted) throw InvalidReport("CSV có dấu nháy chưa được đóng.");
        if (value.Length > 0 || record.Count > 0)
        {
            record.Add(value.ToString());
            records.Add(record);
        }

        return records;
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (character is 'đ' or 'Đ')
            {
                builder.Append('d');
                continue;
            }

            if (CharUnicodeInfo.GetUnicodeCategory(character) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpaceSeparator ||
                character is '_' or '-' or '.') continue;
            if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    private static UserFriendlyException InvalidReport(string message) =>
        new(message, code: WebHoanTienDomainErrorCodes.InvalidShopeeReport);

    private static UserFriendlyException InvalidNumber(string? columnName, int? rowNumber) =>
        InvalidReport(rowNumber.HasValue
            ? $"Dòng {rowNumber}: {columnName ?? "Giá trị số"} không hợp lệ."
            : "Giá trị số không hợp lệ.");

    private sealed record ReportRow(
        string ConversionId,
        string OrderId,
        string AttributionValue,
        DateTime PurchaseTime,
        AffiliateOrderStatus Status,
        string ProviderStatus,
        decimal PurchaseAmount,
        decimal NetCommission,
        string ItemId,
        string ModelId,
        string ProductName,
        int Quantity,
        decimal RefundAmount,
        bool IsFraud);
}

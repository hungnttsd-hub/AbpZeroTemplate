using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Integrations.Shopee;

public sealed record ShopeeCanonicalSettlementRow(
    string SourceAffiliateId,
    string ValidationId,
    string PayoutId,
    DateTime PaidAt,
    DateTime? OrderCompletedFrom,
    DateTime? OrderCompletedTo,
    decimal BillEligibleCommission,
    decimal BillAfterServiceFeeCommission,
    decimal BillPaidCommission,
    string ExternalOrderId,
    decimal OrderEligibleCommission,
    decimal AllocatedServiceFee,
    decimal AllocatedTax,
    decimal ActualPaidCommission);

public sealed record ShopeeCanonicalSettlementReport(
    int RowCount,
    IReadOnlyList<ShopeeCanonicalSettlementRow> Rows);

public class ShopeeCanonicalSettlementReportParser : ITransientDependency
{
    public const string SchemaVersion = "catsback-settlement-v1";

    public async Task<ShopeeCanonicalSettlementReport> ParseAsync(Stream stream,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content)) throw Invalid("File đối soát đang trống.");

        var records = ParseCsv(content, DetectDelimiter(content));
        if (records.Count < 2) throw Invalid("File đối soát phải có header và ít nhất một dòng dữ liệu.");
        var columns = records[0].Select((value, index) => new { Key = Normalize(value), Index = index })
            .Where(value => !string.IsNullOrWhiteSpace(value.Key))
            .GroupBy(value => value.Key, StringComparer.Ordinal)
            .ToDictionary(value => value.Key, value => value.First().Index, StringComparer.Ordinal);

        var schemaColumn = Required(columns, "schema_version");
        var affiliateColumn = Required(columns, "source_affiliate_id");
        var validationColumn = Required(columns, "validation_id");
        var payoutColumn = Required(columns, "payout_id");
        var paidAtColumn = Required(columns, "payment_completed_at_utc");
        var completedFromColumn = Required(columns, "order_completed_from_utc");
        var completedToColumn = Required(columns, "order_completed_to_utc");
        var paymentStatusColumn = Required(columns, "payment_status");
        var validationPayoutStatusColumn = Required(columns, "validation_payout_status");
        var adjustmentColumn = Required(columns, "has_adjustment");
        var clawbackColumn = Required(columns, "has_clawback");
        var cumulativeColumn = Required(columns, "is_cumulative");
        var billEligibleColumn = Required(columns, "bill_eligible_commission");
        var billAfterServiceColumn = Required(columns, "bill_after_service_fee");
        var billPaidColumn = Required(columns, "bill_paid_commission");
        var orderColumn = Required(columns, "order_id");
        var orderEligibleColumn = Required(columns, "order_eligible_commission");
        var serviceFeeColumn = Required(columns, "allocated_service_fee");
        var taxColumn = Required(columns, "allocated_tax");
        var paidColumn = Required(columns, "actual_paid_commission");

        var rows = new List<ShopeeCanonicalSettlementRow>();
        for (var index = 1; index < records.Count; index++)
        {
            var record = records[index];
            if (record.All(string.IsNullOrWhiteSpace)) continue;
            var rowNumber = index + 1;
            if (!string.Equals(Value(record, schemaColumn), SchemaVersion, StringComparison.Ordinal))
                throw Invalid($"Dòng {rowNumber}: schema_version không được hỗ trợ.");
            if (Integer(record, paymentStatusColumn, rowNumber) != 4 ||
                Integer(record, validationPayoutStatusColumn, rowNumber) != 2)
                throw Invalid($"Dòng {rowNumber}: bảng kê chưa ở trạng thái đã thanh toán.");
            if (Boolean(record, adjustmentColumn, rowNumber) || Boolean(record, clawbackColumn, rowNumber) ||
                Boolean(record, cumulativeColumn, rowNumber))
                throw Invalid($"Dòng {rowNumber}: bảng kê có điều chỉnh, truy thu hoặc thanh toán cộng dồn chưa được hỗ trợ.");

            var affiliateId = RequiredValue(record, affiliateColumn, rowNumber, "source_affiliate_id",
                WebHoanTienConsts.AffiliateIdMaxLength);
            var validationId = RequiredValue(record, validationColumn, rowNumber, "validation_id", 64);
            if (validationId.Any(character => character is < '0' or > '9'))
                throw Invalid($"Dòng {rowNumber}: validation_id không hợp lệ.");
            var payoutId = RequiredValue(record, payoutColumn, rowNumber, "payout_id", 128);
            var externalOrderId = RequiredValue(record, orderColumn, rowNumber, "order_id", 256);
            rows.Add(new ShopeeCanonicalSettlementRow(
                affiliateId,
                validationId,
                payoutId,
                Date(record, paidAtColumn, rowNumber, required: true)!.Value,
                Date(record, completedFromColumn, rowNumber, required: false),
                Date(record, completedToColumn, rowNumber, required: false),
                Money(record, billEligibleColumn, rowNumber),
                Money(record, billAfterServiceColumn, rowNumber),
                Money(record, billPaidColumn, rowNumber),
                externalOrderId,
                Money(record, orderEligibleColumn, rowNumber),
                Money(record, serviceFeeColumn, rowNumber),
                Money(record, taxColumn, rowNumber),
                Money(record, paidColumn, rowNumber)));
        }

        if (rows.Count == 0) throw Invalid("File đối soát không có dòng dữ liệu hợp lệ.");
        Validate(rows);
        return new ShopeeCanonicalSettlementReport(rows.Count, rows);
    }

    private static void Validate(IReadOnlyList<ShopeeCanonicalSettlementRow> rows)
    {
        foreach (var bill in rows.GroupBy(row => new { row.SourceAffiliateId, row.ValidationId }))
        {
            var first = bill.First();
            if (bill.Any(row => row.PayoutId != first.PayoutId || row.PaidAt != first.PaidAt ||
                    row.OrderCompletedFrom != first.OrderCompletedFrom ||
                    row.OrderCompletedTo != first.OrderCompletedTo ||
                    row.BillEligibleCommission != first.BillEligibleCommission ||
                    row.BillAfterServiceFeeCommission != first.BillAfterServiceFeeCommission ||
                    row.BillPaidCommission != first.BillPaidCommission))
                throw Invalid($"Bảng kê {first.ValidationId}: metadata không đồng nhất giữa các dòng.");
            if (first.BillEligibleCommission < first.BillAfterServiceFeeCommission ||
                first.BillAfterServiceFeeCommission < first.BillPaidCommission)
                throw Invalid($"Bảng kê {first.ValidationId}: tổng tiền sau phí hoặc sau thuế không hợp lệ.");
            if (bill.GroupBy(row => row.ExternalOrderId, StringComparer.Ordinal).Any(group => group.Count() > 1))
                throw Invalid($"Bảng kê {first.ValidationId}: có ID đơn hàng bị trùng.");

            var eligible = bill.Sum(row => row.OrderEligibleCommission);
            var serviceFee = bill.Sum(row => row.AllocatedServiceFee);
            var tax = bill.Sum(row => row.AllocatedTax);
            var paid = bill.Sum(row => row.ActualPaidCommission);
            if (!Close(eligible, first.BillEligibleCommission) ||
                !Close(serviceFee, first.BillEligibleCommission - first.BillAfterServiceFeeCommission) ||
                !Close(tax, first.BillAfterServiceFeeCommission - first.BillPaidCommission) ||
                !Close(paid, first.BillPaidCommission))
                throw Invalid($"Bảng kê {first.ValidationId}: tổng các đơn không khớp tổng thanh toán.");
            if (bill.Any(row => !Close(row.OrderEligibleCommission - row.AllocatedServiceFee - row.AllocatedTax,
                    row.ActualPaidCommission)))
                throw Invalid($"Bảng kê {first.ValidationId}: có dòng phân bổ phí hoặc thuế không cân bằng.");
        }

        var duplicateOrder = rows.GroupBy(row => row.ExternalOrderId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateOrder is not null)
            throw Invalid($"Đơn hàng {duplicateOrder.Key} xuất hiện trong nhiều bảng kê thanh toán.");
    }

    private static bool Close(decimal left, decimal right)
    {
        var tolerance = Math.Max(1m, Math.Abs(right) * 0.0001m);
        return Math.Abs(left - right) <= tolerance;
    }

    private static decimal Money(IReadOnlyList<string> row, int column, int rowNumber)
    {
        var value = Value(row, column);
        if (!decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out var result) || result < 0m)
            throw Invalid($"Dòng {rowNumber}: giá trị tiền không hợp lệ.");
        return decimal.Round(result, 4, MidpointRounding.AwayFromZero);
    }

    private static int Integer(IReadOnlyList<string> row, int column, int rowNumber) =>
        int.TryParse(Value(row, column), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw Invalid($"Dòng {rowNumber}: trạng thái không hợp lệ.");

    private static bool Boolean(IReadOnlyList<string> row, int column, int rowNumber)
    {
        var value = Value(row, column);
        if (bool.TryParse(value, out var result)) return result;
        if (value == "0") return false;
        if (value == "1") return true;
        throw Invalid($"Dòng {rowNumber}: giá trị boolean không hợp lệ.");
    }

    private static DateTime? Date(IReadOnlyList<string> row, int column, int rowNumber, bool required)
    {
        var value = Value(row, column);
        if (string.IsNullOrWhiteSpace(value))
        {
            if (!required) return null;
            throw Invalid($"Dòng {rowNumber}: thiếu thời gian thanh toán.");
        }
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                out var result))
            throw Invalid($"Dòng {rowNumber}: thời gian không hợp lệ.");
        return result.UtcDateTime;
    }

    private static int Required(IReadOnlyDictionary<string, int> columns, string name) =>
        columns.TryGetValue(Normalize(name), out var index)
            ? index
            : throw Invalid($"Không thấy cột bắt buộc: {name}.");

    private static string RequiredValue(IReadOnlyList<string> row, int column, int rowNumber, string name,
        int maxLength)
    {
        var value = Value(row, column);
        if (string.IsNullOrWhiteSpace(value)) throw Invalid($"Dòng {rowNumber}: thiếu {name}.");
        if (value.Length > maxLength) throw Invalid($"Dòng {rowNumber}: {name} dài quá {maxLength} ký tự.");
        return value;
    }

    private static string Value(IReadOnlyList<string> row, int column) =>
        column < row.Count ? row[column].Trim() : string.Empty;

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
        if (quoted) throw Invalid("CSV có dấu nháy chưa được đóng.");
        if (value.Length > 0 || record.Count > 0) { record.Add(value.ToString()); records.Add(record); }
        return records;
    }

    private static string Normalize(string value)
    {
        if (!string.IsNullOrEmpty(value) && value[0] == '\ufeff') value = value[1..];
        return new string(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    }

    private static UserFriendlyException Invalid(string message) =>
        new(message, code: WebHoanTienDomainErrorCodes.InvalidShopeeSettlementReport);
}

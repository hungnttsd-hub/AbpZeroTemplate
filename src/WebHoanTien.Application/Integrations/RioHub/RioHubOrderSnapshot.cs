using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Integrations.RioHub;

// Only field names documented in the supplied 09/10/2026 spec are read.
internal sealed class RioHubOrderSnapshot
{
    public string OrderId { get; private init; } = string.Empty;
    public string SkuId { get; private init; } = string.Empty;
    private string? ProductId { get; init; }
    private string? ProductName { get; init; }
    private string? SubId { get; init; }
    private string? TraceId { get; init; }
    private string? TraceType { get; init; }
    private int Status { get; init; }
    private string Currency { get; init; } = string.Empty;
    private decimal EstimatedCommission { get; init; }
    private decimal? ActualCommission { get; init; }
    private int Quantity { get; init; }
    private int RefundedQuantity { get; init; }
    private bool FullyRefunded { get; init; }
    private string? SettlementStatus { get; init; }
    private DateTime CreatedAtUtc { get; init; }
    private DateTime? UpdatedAtUtc { get; init; }
    private DateTime? SettledAtUtc { get; init; }
    private string RawJson { get; init; } = "{}";

    public static RioHubOrderSnapshot Parse(JsonElement row)
    {
        if (row.ValueKind != JsonValueKind.Object) throw Invalid("orders[]");
        var status = Integer(row, "status");
        var quantity = Integer(row, "quantity");
        var refunded = Integer(row, "refunded_quantity");
        var fullyRefunded = Integer(row, "fully_refunded");
        if (status is < 1 or > 3 || quantity < 0 || refunded < 0 || fullyRefunded is < 0 or > 1)
            throw Invalid("status/quantity/refunded_quantity/fully_refunded");
        return new RioHubOrderSnapshot
        {
            OrderId = RequiredText(row, "order_id", 128), SkuId = RequiredText(row, "sku_id", 128),
            ProductId = Text(row, "product_id", 128), ProductName = Text(row, "product_name", 1000),
            SubId = Text(row, "sub_id", 128), TraceId = Text(row, "trace_id", 256), TraceType = Text(row, "trace_type", 32),
            Status = status, Currency = RequiredText(row, "currency", 16),
            EstimatedCommission = Money(row, "est_commission", required: true)!.Value,
            ActualCommission = Money(row, "actual_commission", required: false),
            Quantity = quantity, RefundedQuantity = refunded, FullyRefunded = fullyRefunded == 1,
            SettlementStatus = Text(row, "settlement_status", 128),
            CreatedAtUtc = Time(row, "create_time", "time_created_iso", "time_created") ?? throw Invalid("create_time"),
            UpdatedAtUtc = Time(row, "update_time", null, null),
            SettledAtUtc = Time(row, null, "settled_at_iso", "settled_at"), RawJson = row.GetRawText()
        };
    }

    public bool ApplyTo(RioHubOrderSku entity, DateTime fetchedAtUtc) => entity.Apply(
        ProductId, ProductName, SubId, TraceId, TraceType, Status, Currency, EstimatedCommission, ActualCommission,
        Quantity, RefundedQuantity, FullyRefunded, SettlementStatus, CreatedAtUtc, UpdatedAtUtc, SettledAtUtc, fetchedAtUtc, RawJson);

    private static string RequiredText(JsonElement row, string name, int maximum) =>
        Text(row, name, maximum) is { Length: > 0 } value ? value : throw Invalid(name);

    private static string? Text(JsonElement row, string name, int maximum)
    {
        if (!row.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.String || value.GetString()!.Length > maximum) throw Invalid(name);
        return value.GetString();
    }

    internal static int Integer(JsonElement row, string name)
    {
        if (!row.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
            throw Invalid(name);
        return number;
    }

    private static decimal? Money(JsonElement row, string name, bool required)
    {
        var text = Text(row, name, 100);
        if (text is null && !required) return null;
        if (text is null || !Regex.IsMatch(text, @"\A-?[0-9]+(?:\.[0-9]+)?\z", RegexOptions.CultureInvariant) ||
            !decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var amount)) throw Invalid(name);
        // Reject values that decimal parsing would silently round beyond its capacity.
        var canonical = text.TrimStart('-').Split('.');
        var integerPart = canonical[0].TrimStart('0');
        var fraction = canonical.Length == 2 ? canonical[1].TrimEnd('0') : string.Empty;
        if (fraction.Length > 28 || integerPart.Length + fraction.Length > 29 ||
            amount.ToString("0.############################", CultureInfo.InvariantCulture).TrimStart('-') !=
            (integerPart.Length == 0 ? "0" : integerPart) + (fraction.Length > 0 ? "." + fraction : "")) throw Invalid(name);
        return amount;
    }

    private static DateTime? Time(JsonElement row, string? unixField, string? isoField, string? bareField)
    {
        if (unixField is not null && row.TryGetProperty(unixField, out var unix) && unix.ValueKind != JsonValueKind.Null)
        {
            if (unix.ValueKind != JsonValueKind.Number || !unix.TryGetInt64(out var seconds)) throw Invalid(unixField);
            try { return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime; }
            catch (ArgumentOutOfRangeException) { throw Invalid(unixField); }
        }
        if (isoField is not null && Text(row, isoField, 64) is { Length: > 0 } iso)
        {
            if (!DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var time)) throw Invalid(isoField);
            return time.UtcDateTime;
        }
        if (bareField is not null && Text(row, bareField, 64) is { Length: > 0 } bare)
        {
            if (!DateTime.TryParseExact(bare, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var time)) throw Invalid(bareField);
            return time;
        }
        return null;
    }

    private static InvalidDataException Invalid(string field) =>
        new($"RioHub orders: field '{field}' thiếu hoặc sai định dạng. Không tiến watermark.");
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Volo.Abp;

namespace WebHoanTien.TikTokAffiliate;

// Search Creator Affiliate Orders 202410: pagination is over SKU rows, not order objects.
// Collect every page before persisting, so a partial page cannot erase the rest of an order.
internal sealed class TikTokCreatorOrderSnapshot
{
    public string Id { get; }
    public DateTime CreatedAt { get; }
    public string Status { get; }
    private readonly JsonElement _header;
    public Dictionary<string, JsonElement> Skus { get; } = new(StringComparer.Ordinal);

    private TikTokCreatorOrderSnapshot(string id, DateTime createdAt, string status, JsonElement header)
    { Id = id; CreatedAt = createdAt; Status = status; _header = header.Clone(); }

    public string ToJson()
    {
        // Retain other documented order fields (such as delivery_time) and every merged SKU.
        var data = _header.EnumerateObject().Where(x => x.Name != "skus")
            .ToDictionary(x => x.Name, x => (object)x.Value.Clone(), StringComparer.Ordinal);
        data["skus"] = Skus.Values.ToArray();
        return JsonSerializer.Serialize(data);
    }

    public static IReadOnlyList<TikTokCreatorOrderSnapshot> Merge(IEnumerable<JsonElement> rows,
        long fromUnix, long toUnix)
    {
        var result = new Dictionary<string, TikTokCreatorOrderSnapshot>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var id = RequiredString(row, "id");
            if (!row.TryGetProperty("create_time", out var time) || time.ValueKind != JsonValueKind.Number || !time.TryGetInt64(out var unix)
                || unix < fromUnix || unix >= toUnix)
                throw InvalidResponse();
            var status = RequiredString(row, "status");
            var createdAt = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
            if (!result.TryGetValue(id, out var order))
                result[id] = order = new(id, createdAt, status, row);
            if (order.Status != status || order.CreatedAt != createdAt)
                throw new UserFriendlyException("Dữ liệu TikTok thay đổi giữa các trang. Chọn khoảng ngày nhỏ hơn và đồng bộ lại; chưa cập nhật ví.");
            if (!row.TryGetProperty("skus", out var skus) || skus.ValueKind != JsonValueKind.Array || skus.GetArrayLength() == 0)
                throw InvalidResponse();
            foreach (var sku in skus.EnumerateArray())
            {
                var skuId = RequiredString(sku, "id");
                // Conflicting copies (including different click traces) must not overwrite one another.
                if (order.Skus.TryGetValue(skuId, out var previous) && previous.GetRawText() != sku.GetRawText())
                    throw new UserFriendlyException("TikTok trả nhiều dữ liệu khác nhau cho cùng SKU trong một đơn. Chưa cập nhật đơn hoặc ví; hãy đồng bộ lại.");
                order.Skus[skuId] = sku.Clone();
            }
        }
        return result.Values.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
    }

    public static string? String(JsonElement value, string field) => value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty(field, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() : null;

    public static string RequiredString(JsonElement value, string field)
    {
        var text = String(value, field);
        return !string.IsNullOrWhiteSpace(text) && text.Length <= 128 ? text : throw InvalidResponse();
    }

    public static bool TryMoney(JsonElement sku, string field, out decimal amount)
    {
        amount = 0;
        if (!sku.TryGetProperty(field, out var money) || money.ValueKind != JsonValueKind.Object
            || String(money, "currency") != "VND") return false;
        var text = String(money, "amount");
        // No currency conversion, cents assumption or ambiguous thousands separator parsing.
        return text is { Length: > 0 and <= 24 } && text.All(c => char.IsAsciiDigit(c) || c == '.')
            && decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount)
            && amount >= 0 && amount <= 1_000_000_000_000m;
    }

    public static int Quantity(JsonElement sku) => sku.TryGetProperty("quantity", out var value)
        && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var quantity) && quantity > 0
        ? quantity : throw InvalidResponse();

    private static UserFriendlyException InvalidResponse() => new("TikTok trả về đơn hàng thiếu dữ liệu hợp lệ. Chưa cập nhật đơn hoặc ví.");
}

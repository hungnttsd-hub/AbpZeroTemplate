using System;
using System.Globalization;
using System.Text.Json;
using WebHoanTien.Affiliates;

namespace WebHoanTien.TikTokAffiliate;

internal static class RioHubProductMapper
{
    public static TikTokProductDto Map(JsonElement product, string productId)
    {
        var configured = FindOverride(productId);
        if (configured is not null) return configured;
        var salePrice = Amount(product, "sales_price");
        var price = salePrice ?? Amount(product, "original_price");
        var currency = NestedText(product, salePrice.HasValue ? "sales_price" : "original_price", "currency");
        if (string.IsNullOrWhiteSpace(currency)) currency = NestedText(product, "commission", "currency");
        var rate = NestedNumber(product, "observed_commission", "commission_rate");
        var source = rate.HasValue ? NestedText(product, "observed_commission", "scope") : null;
        if (!rate.HasValue)
        {
            var standard = NestedNumber(product, "commission", "rate");
            if (standard.HasValue) rate = standard + (NestedNumber(product, "shop_ads_commission", "rate") ?? 0m);
        }
        var image = Text(product, "main_image_url") ?? string.Empty;
        if (!Uri.TryCreate(image, UriKind.Absolute, out var uri) || !uri.IsDefaultPort || uri.UserInfo.Length > 0)
            image = string.Empty;
        else if (uri.Scheme == "http") image = new UriBuilder(uri) { Scheme = "https", Port = -1 }.Uri.AbsoluteUri;
        else if (uri.Scheme != "https") image = string.Empty;
        return new TikTokProductDto(productId, Text(product, "title") ?? "Sản phẩm TikTok Shop",
            NestedText(product, "shop", "name") ?? string.Empty, price, currency ?? string.Empty,
            rate.HasValue && rate >= 0m ? rate / 10000m : null, image, source);
    }

    public static TikTokProductDto? FindOverride(string? productId)
    {
        var product = TikTokProductOverrides.Find(productId);
        return product is null ? null : new TikTokProductDto(product.ProductId, product.Title, string.Empty,
            product.Price, "VND", null, product.ImageUrl, "configured", product.CommissionAmount);
    }

    private static decimal? Amount(JsonElement product, string field) => NestedNumber(product, field, "minimum_amount");
    private static string? Text(JsonElement value, string field) =>
        value.TryGetProperty(field, out var text) && text.ValueKind == JsonValueKind.String ? text.GetString() : null;
    private static string? NestedText(JsonElement product, string field, string child) =>
        product.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.Object ? Text(value, child) : null;
    private static decimal? NestedNumber(JsonElement product, string field, string child)
    {
        if (!product.TryGetProperty(field, out var parent) || parent.ValueKind != JsonValueKind.Object ||
            !parent.TryGetProperty(child, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var amount)) return amount;
        return value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(),
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) ? amount : null;
    }
}

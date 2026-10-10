using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WebHoanTien.Web.Integrations;

internal static class TikTokTokenDiagnostics
{
    // An allowlisted summary plus a bounded shape: never serialize the original response.
    public static string Describe(JsonElement root)
    {
        var data = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var value)
            && value.ValueKind == JsonValueKind.Object ? value : default;
        var remaining = 200;
        return JsonSerializer.Serialize(new
        {
            user_type = Number(data, "user_type"),
            access_token_expire_in = Number(data, "access_token_expire_in"),
            refresh_token_expire_in = Number(data, "refresh_token_expire_in"),
            access_token = CredentialSummary(data, "access_token"),
            refresh_token = CredentialSummary(data, "refresh_token"),
            granted_scopes = Scopes(data),
            response_shape = Shape(root, 0, ref remaining)
        });
    }

    private static long? Number(JsonElement data, string name) =>
        data.ValueKind == JsonValueKind.Object && data.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number) ? number : null;

    private static object CredentialSummary(JsonElement data, string name)
    {
        var present = data.ValueKind == JsonValueKind.Object && data.TryGetProperty(name, out _);
        var length = present && data.GetProperty(name).ValueKind == JsonValueKind.String
            ? data.GetProperty(name).GetString()?.Length : null;
        return new { present, length, value = "[REDACTED]" };
    }

    private static string[] Scopes(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object || !data.TryGetProperty("granted_scopes", out var scopes)
            || scopes.ValueKind != JsonValueKind.Array) return Array.Empty<string>();
        return scopes.EnumerateArray().Take(64).Select(scope =>
        {
            var text = scope.ValueKind == JsonValueKind.String ? scope.GetString() : null;
            // Scope names, not arbitrary strings echoed by a remote server.
            return text is { Length: > 0 and <= 128 } && text.Contains('.')
                && (text.StartsWith("creator.", StringComparison.Ordinal) || text.StartsWith("seller.", StringComparison.Ordinal))
                && text.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c is '.' or '_')
                ? text : "[REDACTED]";
        }).ToArray();
    }

    private static object Shape(JsonElement value, int depth, ref int remaining)
    {
        if (--remaining < 0 || depth >= 6) return "[TRUNCATED]";
        if (value.ValueKind == JsonValueKind.Object)
        {
            var fields = new Dictionary<string, object>();
            foreach (var property in value.EnumerateObject().Take(64))
            {
                if (remaining <= 0) break;
                var name = property.Name.Length <= 64
                    && property.Name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')
                    ? property.Name : "[REDACTED_FIELD_NAME]";
                fields[name] = Shape(property.Value, depth + 1, ref remaining);
            }
            return fields;
        }
        if (value.ValueKind == JsonValueKind.Array)
        {
            var items = new List<object>();
            foreach (var item in value.EnumerateArray().Take(10))
            {
                if (remaining <= 0) break;
                items.Add(Shape(item, depth + 1, ref remaining));
            }
            return new { count = value.GetArrayLength(), item_shapes = items };
        }
        // Hide all values, including unknown nested tokens, personal details and error messages.
        return value.ValueKind.ToString();
    }
}

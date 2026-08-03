using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace AbpIoTemplateProject.Web.TagHelpers;

/// <summary>
/// Enhances an ASP.NET Core input with culture-aware number formatting while
/// posting an ungrouped value under the original model field name.
/// </summary>
[HtmlTargetElement("input", Attributes = "asp-for")]
public sealed class CultureNumberInputTagHelper : TagHelper
{
    public override int Order => 1000;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    [HtmlAttributeName("culture-number")]
    public bool Enabled { get; set; } = true;

    [HtmlAttributeName("culture-number-decimals")]
    public int? DecimalDigits { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Enabled || !IsSupportedNumericType(For.Metadata.ModelType))
        {
            output.Attributes.RemoveAll("culture-number");
            output.Attributes.RemoveAll("culture-number-decimals");
            return;
        }

        var culture = CultureInfo.CurrentCulture;
        var digits = Math.Clamp(DecimalDigits ?? GetDefaultDecimalDigits(For.Metadata.ModelType), 0, 20);
        var originalName = output.Attributes["name"]?.Value?.ToString() ?? For.Name;
        var originalId = output.Attributes["id"]?.Value?.ToString() ?? originalName.Replace('.', '_');
        var rawId = originalId + "__raw";

        output.Attributes.SetAttribute("type", "text");
        output.Attributes.SetAttribute("name", originalName + "__display");
        output.Attributes.SetAttribute("id", originalId);
        output.Attributes.SetAttribute("value", FormatDisplayValue(For.Model, digits, culture));
        output.Attributes.SetAttribute("inputmode", digits == 0 ? "numeric" : "decimal");
        output.Attributes.SetAttribute("autocomplete", "off");
        output.Attributes.SetAttribute("data-culture-number", culture.Name);
        output.Attributes.SetAttribute("data-culture-number-decimals", digits.ToString(CultureInfo.InvariantCulture));
        output.Attributes.SetAttribute("data-culture-number-target", rawId);
        AddCssClass(output, "js-culture-number");

        foreach (var attributeName in output.Attributes
                     .Where(x => x.Name.StartsWith("data-val", StringComparison.OrdinalIgnoreCase))
                     .Select(x => x.Name)
                     .ToList())
        {
            output.Attributes.RemoveAll(attributeName);
        }

        output.Attributes.RemoveAll("min");
        output.Attributes.RemoveAll("max");
        output.Attributes.RemoveAll("step");
        output.Attributes.RemoveAll("culture-number");
        output.Attributes.RemoveAll("culture-number-decimals");

        var hiddenInput = new TagBuilder("input");
        hiddenInput.TagRenderMode = TagRenderMode.SelfClosing;
        hiddenInput.Attributes["type"] = "hidden";
        hiddenInput.Attributes["id"] = rawId;
        hiddenInput.Attributes["name"] = originalName;
        hiddenInput.Attributes["value"] = FormatRawValue(For.Model, culture);
        output.PostElement.AppendHtml(hiddenInput);
    }

    private static int GetDefaultDecimalDigits(Type modelType)
    {
        var type = Nullable.GetUnderlyingType(modelType) ?? modelType;
        return type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)
            ? 0
            : 2;
    }

    private static bool IsSupportedNumericType(Type modelType)
    {
        var type = Nullable.GetUnderlyingType(modelType) ?? modelType;
        return type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) || type == typeof(decimal);
    }

    private static string FormatDisplayValue(object? value, int digits, CultureInfo culture)
    {
        return value is IFormattable formattable
            ? formattable.ToString($"N{digits}", culture) ?? string.Empty
            : string.Empty;
    }

    private static string FormatRawValue(object? value, CultureInfo culture)
    {
        return value is IFormattable formattable
            ? formattable.ToString(null, culture) ?? string.Empty
            : string.Empty;
    }

    private static void AddCssClass(TagHelperOutput output, string cssClass)
    {
        var current = output.Attributes["class"]?.Value?.ToString();
        output.Attributes.SetAttribute("class", string.IsNullOrWhiteSpace(current) ? cssClass : current + " " + cssClass);
    }
}

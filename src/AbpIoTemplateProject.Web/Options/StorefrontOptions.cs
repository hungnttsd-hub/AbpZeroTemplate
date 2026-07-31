namespace AbpIoTemplateProject.Web.Options;

public sealed class StorefrontOptions
{
    public const string SectionName = "Storefront";

    public string BrandName { get; set; } = "AquaHome";

    public string Hotline { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string SupportHours { get; set; } = string.Empty;

    public string StoreMapUrl { get; set; } = string.Empty;
}

namespace WebHoanTien.Web.Pages.Shared;

public sealed class CustomerSectionHeaderModel
{
    public string Title { get; init; } = string.Empty;
    public string? TitleId { get; init; }
    public string? Description { get; init; }
    public string? ActionUrl { get; init; }
    public string ActionLabel { get; init; } = "Xem tất cả";
}

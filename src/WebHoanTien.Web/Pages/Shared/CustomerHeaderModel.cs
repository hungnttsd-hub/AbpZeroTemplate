namespace WebHoanTien.Web.Pages.Shared;

/// <summary>Shared header content; the caller owns the surrounding header/hero layout.</summary>
public sealed class CustomerHeaderModel
{
    public string? BackUrl { get; init; }
    public string BackLabel { get; init; } = "Quay về trang chủ";
    public string? Title { get; init; }
    public bool IsSiteHeader { get; init; }
    public bool IsWalletActive { get; init; }
    public bool IsOrdersActive { get; init; }
    public int UnreadNotificationCount { get; init; }
}

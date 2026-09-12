namespace WebHoanTien.Web.Pages.Shared;

public sealed class CustomerDesktopShellModel
{
    public string CurrentPath { get; init; } = "/";
    public bool IsAuthenticated { get; init; }
    public string DisplayName { get; init; } = "Tài khoản";
    public string Initial { get; init; } = "C";
    public int UnreadNotificationCount { get; init; }
    public System.Collections.Generic.IReadOnlyList<CustomerDesktopNavigationItem> AdminNavigationItems { get; init; }
        = System.Array.Empty<CustomerDesktopNavigationItem>();
}

public sealed record CustomerDesktopNavigationItem(string Url, string Label, string Icon, bool FullReload = false);

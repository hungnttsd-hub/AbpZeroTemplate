namespace WebHoanTien.Web.Pages;

public static class CustomerLinkUi
{
    public static string? MoneyRange(decimal? minimum, decimal? maximum = null)
    {
        if (!minimum.HasValue) return null;
        return maximum.HasValue && maximum.Value > minimum.Value
            ? $"{minimum.Value:N0}–{maximum.Value:N0}₫" : $"{minimum.Value:N0}₫";
    }
}

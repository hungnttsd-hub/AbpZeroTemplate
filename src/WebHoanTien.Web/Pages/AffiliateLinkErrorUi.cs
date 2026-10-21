using Volo.Abp;

namespace WebHoanTien.Web.Pages;

internal static class AffiliateLinkErrorUi
{
    public const string DefaultMessage = "Không thể tạo link mua hàng lúc này. Vui lòng thử lại sau.";

    public static string MessageFor(BusinessException exception) => exception.Code switch
    {
        WebHoanTienDomainErrorCodes.InvalidAffiliateUrl when
            exception.Data["Reason"] as string == "ShortLinkTargetNotFound" =>
            "Chưa đọc được đích đến của link rút gọn Shopee. Hãy mở link trên Shopee và sao chép link đầy đủ của sản phẩm hoặc cửa hàng.",
        WebHoanTienDomainErrorCodes.InvalidAffiliateUrl =>
            "Link Shopee không hợp lệ. Vui lòng sao chép lại link sản phẩm hoặc cửa hàng.",
        WebHoanTienDomainErrorCodes.UnsafeRedirect =>
            "Link Shopee chuyển hướng đến địa chỉ không được hỗ trợ. Vui lòng dùng link đầy đủ của sản phẩm hoặc cửa hàng trên shopee.vn.",
        WebHoanTienDomainErrorCodes.ProviderNotConfigured =>
            "Chưa cấu hình dịch vụ tạo link. Vui lòng liên hệ quản trị viên để kiểm tra.",
        _ => DefaultMessage
    };
}

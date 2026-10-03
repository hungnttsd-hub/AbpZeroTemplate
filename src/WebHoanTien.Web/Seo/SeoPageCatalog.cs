using System;
using System.Collections.Generic;
using System.Linq;
using WebHoanTien.Web.Pages.Guide;

namespace WebHoanTien.Web.Seo;

public static class SeoPageCatalog
{
    public static IReadOnlyList<SeoPage> Pages { get; } = new[]
    {
        new SeoPage("/", "", ""),
        new SeoPage("/About", "Giới thiệu CatBack", "Tìm hiểu CatBack, cách tạo link tiếp thị liên kết, theo dõi đơn hàng và nhận hoàn tiền từ hoa hồng hợp lệ."),
        new SeoPage("/Contact", "Contact — Liên hệ CatBack", "Liên hệ CatBack để được hỗ trợ tài khoản, đơn hàng, hoàn tiền, yêu cầu xóa dữ liệu và hợp tác tích hợp."),
        new SeoPage("/huong-dan", "Hướng dẫn mua Shopee hoàn tiền", "Hướng dẫn cài đặt CatBack, đăng ký tài khoản và tạo link mua hàng Shopee để theo dõi hoàn tiền."),
        new SeoPage("/Legal/Terms", "Terms of Service — Điều khoản sử dụng", "Điều khoản CatBack về tài khoản, tiếp thị liên kết, ghi nhận hoa hồng, rút tiền và trách nhiệm khi sử dụng dịch vụ."),
        new SeoPage("/Legal/Privacy", "Privacy Policy — Chính sách quyền riêng tư", "Cách CatBack thu thập, sử dụng, chia sẻ, lưu trữ dữ liệu cá nhân và tiếp nhận yêu cầu xóa tài khoản, dữ liệu.")
    }.Concat(GuideVideoCatalog.All.Select(video => new SeoPage(
        "/huong-dan/video/" + video.Slug, video.Title, video.Subtitle + ". Xem video và các bước thực hiện trên CatBack."))).ToArray();

    public static SeoPage? Find(string? path)
    {
        var normalized = string.IsNullOrEmpty(path) ? "/" : path.TrimEnd('/');
        if (normalized.Length == 0 || normalized.Equals("/Index", StringComparison.OrdinalIgnoreCase)) normalized = "/";
        return Pages.FirstOrDefault(page => page.Path.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }
}

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
        new SeoPage("/huong-dan", "Hướng dẫn mua Shopee hoàn tiền", "Hướng dẫn cài đặt CatBack, đăng ký tài khoản và tạo link mua hàng Shopee để theo dõi hoàn tiền."),
        new SeoPage("/Legal/Terms", "Điều khoản sử dụng", "Tìm hiểu điều khoản sử dụng CatBack, quy định ghi nhận hoa hồng và trách nhiệm khi sử dụng dịch vụ."),
        new SeoPage("/Legal/Privacy", "Chính sách riêng tư", "Tìm hiểu cách CatBack xử lý dữ liệu tài khoản, thông tin nhận tiền và dữ liệu affiliate để vận hành dịch vụ.")
    }.Concat(GuideVideoCatalog.All.Select(video => new SeoPage(
        "/huong-dan/video/" + video.Slug, video.Title, video.Subtitle + ". Xem video và các bước thực hiện trên CatBack."))).ToArray();

    public static SeoPage? Find(string? path)
    {
        var normalized = string.IsNullOrEmpty(path) ? "/" : path.TrimEnd('/');
        if (normalized.Length == 0 || normalized.Equals("/Index", StringComparison.OrdinalIgnoreCase)) normalized = "/";
        return Pages.FirstOrDefault(page => page.Path.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace WebHoanTien.Web.Pages.Guide;

public sealed record GuideVideoSources(string Android, string Ios, string Fallback);

public sealed record GuideVideoDefinition(
    string Slug,
    string AnchorId,
    string Illustration,
    string Title,
    string Subtitle,
    string AndroidSubtitle,
    string IosSubtitle,
    string Duration,
    string MetaLabel,
    string AndroidMetaLabel,
    string IosMetaLabel,
    GuideVideoSources Sources,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> AndroidSteps,
    IReadOnlyList<string> IosSteps,
    string CtaText,
    string? NextSlug);

public static class GuideVideoCatalog
{
    private const string InstallAndroidVideoId = "WfM1OAOIjoY";
    private const string InstallIosDemoVideoId = "i1hrkRoIGvo";
    private const string RegisterVideoId = "23cF8fGyC3I";
    private const string CreateLinkVideoId = "5RcV8wuFvH4";

    public static IReadOnlyList<GuideVideoDefinition> All { get; } =
    [
        new(
            "cai-dat",
            "cai-dat",
            "install",
            "Cài đặt ứng dụng / thêm lối tắt",
            "Dành cho thiết bị bạn đang sử dụng",
            "Dành cho thiết bị Android đang sử dụng",
            "Dành cho thiết bị iPhone đang sử dụng",
            "01:20",
            "Thiết bị",
            "Android",
            "iPhone",
            new GuideVideoSources(InstallAndroidVideoId, InstallIosDemoVideoId, InstallIosDemoVideoId),
            [
                "Mở CatBack bằng trình duyệt trên thiết bị của bạn.",
                "Mở menu chia sẻ hoặc menu trình duyệt.",
                "Chọn thêm CatBack vào màn hình chính.",
                "Xác nhận để hoàn tất và mở CatBack từ biểu tượng mới."
            ],
            [
                "Mở CatBack bằng Chrome hoặc trình duyệt Android bạn đang sử dụng.",
                "Bấm menu ba chấm ở góc trên của trình duyệt.",
                "Chọn Cài đặt ứng dụng hoặc Thêm vào màn hình chính.",
                "Xác nhận để thêm biểu tượng CatBack ra màn hình chính."
            ],
            [
                "Mở CatBack bằng Safari trên iPhone hoặc iPad.",
                "Bấm nút Chia sẻ, biểu tượng hình vuông có mũi tên hướng lên.",
                "Kéo xuống và chọn Thêm vào Màn hình chính.",
                "Kiểm tra tên CatBack rồi bấm Thêm để hoàn tất."
            ],
            "Xem video tiếp theo",
            "tao-link-hoan-tien"),
        new(
            "tao-link-hoan-tien",
            "tao-link-hoan-tien",
            "link",
            "Tạo link hoàn tiền",
            "Cách dán link và tạo link nhanh",
            "Cách dán link và tạo link nhanh",
            "Cách dán link và tạo link nhanh",
            "00:45",
            "Shopee",
            "Shopee",
            "Shopee",
            new GuideVideoSources(CreateLinkVideoId, CreateLinkVideoId, CreateLinkVideoId),
            [
                "Sao chép link sản phẩm Shopee bạn muốn mua.",
                "Dán link vào ô tạo link hoàn tiền trên CatBack.",
                "Bấm Tạo link hoàn tiền để hệ thống xử lý.",
                "Bấm Mua ngay từ link mới và theo dõi đơn hàng trong mục Đơn hàng."
            ],
            [],
            [],
            "Xem video tiếp theo",
            "dang-ky"),
        new(
            "dang-ky",
            "dang-ky",
            "register",
            "Đăng ký tài khoản",
            "Tạo tài khoản CatBack trong vài bước",
            "Tạo tài khoản CatBack trong vài bước",
            "Tạo tài khoản CatBack trong vài bước",
            "01:10",
            "Tài khoản",
            "Tài khoản",
            "Tài khoản",
            new GuideVideoSources(RegisterVideoId, RegisterVideoId, RegisterVideoId),
            [
                "Chọn Đăng ký miễn phí trên màn hình đăng nhập.",
                "Nhập email và mật khẩu từ 6 ký tự hoặc chọn Tiếp tục với Google.",
                "Hoàn tất tạo tài khoản theo hướng dẫn hiển thị trên màn hình.",
                "Đăng nhập để bắt đầu tạo link và theo dõi tiền hoàn của bạn."
            ],
            [],
            [],
            "Bắt đầu ngay",
            null)
    ];

    public static GuideVideoDefinition? Find(string? slug)
    {
        return All.FirstOrDefault(video =>
            string.Equals(video.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }
}

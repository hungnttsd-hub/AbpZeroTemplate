using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Store;

namespace AbpIoTemplateProject.Web.Pages;

public class ContentPageModel : AbpIoTemplateProjectPageModel
{
    private readonly ICartAppService _cartAppService;
    public string Title { get; private set; } = string.Empty;
    public string BodyHtml { get; private set; } = string.Empty;

    private static readonly IReadOnlyDictionary<string, (string Title, string Content)> Pages =
        new Dictionary<string, (string, string)>
        {
            ["how-to-buy"] = ("Hướng dẫn mua hàng", "<p>Chọn sản phẩm, cấu hình phiên bản và số lượng, sau đó kiểm tra giỏ hàng trước khi thanh toán.</p><h2>Đặt hàng</h2><p>Điền đầy đủ thông tin nhận hàng và chọn phương thức thanh toán. Mã đơn được tạo ngay sau khi xác nhận.</p>"),
            ["payment-guide"] = ("Hướng dẫn thanh toán", "<p>Aqua Garden hỗ trợ thanh toán khi nhận hàng, chuyển khoản và cổng thanh toán trực tuyến khi được cấu hình.</p><p>Không chuyển khoản tới tài khoản ngoài thông tin xác nhận chính thức của cửa hàng.</p>"),
            ["shipping-policy"] = ("Chính sách vận chuyển", "<p>Đơn được đóng gói phù hợp với từng nhóm sản phẩm. Thời gian dự kiến hiển thị tại bước thanh toán.</p><p>Khách hàng kiểm tra tình trạng kiện hàng và liên hệ ngay khi phát hiện hư hỏng.</p>"),
            ["return-policy"] = ("Chính sách đổi trả", "<p>Yêu cầu đổi trả cần kèm mã đơn, ảnh hoặc video mở kiện và được gửi trong thời hạn áp dụng của từng sản phẩm.</p><p>Sản phẩm sống, hàng tiêu hao hoặc đã sử dụng có điều kiện xử lý riêng.</p>"),
            ["privacy-policy"] = ("Chính sách bảo mật", "<p>Thông tin khách hàng chỉ được sử dụng để xử lý đơn, hỗ trợ và thực hiện nghĩa vụ pháp lý.</p><p>Aqua Garden không bán dữ liệu cá nhân cho bên thứ ba.</p>"),
            ["terms"] = ("Điều khoản dịch vụ", "<p>Giá, tồn kho và ưu đãi được xác nhận lại ở phía máy chủ khi đặt hàng. Aqua Garden có thể liên hệ để làm rõ thông tin đơn bất thường.</p>"),
            ["warranty-policy"] = ("Chính sách bảo hành", "<p>Thời hạn bảo hành được ghi trên trang sản phẩm và chứng từ đơn hàng. Vui lòng giữ nguyên tem, mã sản phẩm và phụ kiện đi kèm.</p>")
        };

    public ContentPageModel(ICartAppService cartAppService) { _cartAppService = cartAppService; }

    public async Task OnGetAsync(string slug)
    {
        if (!Pages.TryGetValue(slug, out var page))
        {
            throw new Volo.Abp.UserFriendlyException("Không tìm thấy trang nội dung.");
        }
        Title = page.Title;
        BodyHtml = page.Content;
        await LoadCartSummaryAsync(_cartAppService);
    }
}

# CatBack Loading UI – Hướng dẫn triển khai cho Codex

## Mục tiêu
Triển khai các trạng thái loading đồng bộ với thiết kế CatBack hiện tại: nền sáng, navy đậm, teal chủ đạo, bo góc mềm, bóng nhẹ. Không dùng loading full-screen cho mọi thao tác; chọn đúng loại loading theo ngữ cảnh để giao diện không bị nặng.

## Asset trong gói
- `00_loading_design_overview.png`: bảng tham chiếu tổng thể.
- `01_page_loading_mascot.png`: loading chuyển trang có mascot CatBack.
- `02_page_loading_minimal.png`: loading chuyển trang tối giản bằng vòng tròn.
- `03_page_loading_progress.png`: loading có tiến trình cho tác vụ lâu.
- `04_button_processing.png`: nút đang xử lý.
- `05_inline_form_loading.png`: loading nhỏ trong form/nút phụ.
- `06_content_block_loading.png`: loading giữa một block nội dung.
- `07_content_overlay_loading.png`: phủ mờ nội dung cũ trong lúc refresh.
- `08_icon_loading.png`: loading rất nhỏ trên icon.
- `09_skeleton_list.png`: skeleton cho danh sách link/sản phẩm.
- `10_modal_processing.png`: loading trong modal/popup.
- `11_dot_loading.png`, `12_ring_loading.png`, `13_progress_bar.png`: biến thể loader.

## Quy tắc sử dụng
1. **Chuyển trang / tải màn lần đầu**: ưu tiên `01` hoặc `02`. Chỉ dùng overlay toàn màn hình khi route mới chưa có nội dung để hiển thị.
2. **Danh sách link/sản phẩm**: dùng `09_skeleton_list`; không dùng spinner giữa màn hình nếu có thể dựng skeleton theo layout thật.
3. **Tạo link hoàn tiền / lưu / xoá / copy**: giữ nguyên màn hình, disable control đang chạy và dùng `04` hoặc `05` ngay tại control đó.
4. **Refresh một khu vực**: giữ dữ liệu cũ, phủ mờ nhẹ + loader `07` để tránh layout shift.
5. **Tác vụ > 2–3 giây**: modal `10`; nếu backend có progress thực tế thì hiển thị `03`/`13` với phần trăm thật. Không giả lập % nếu không có dữ liệu.
6. **Không chặn toàn bộ app** cho các thao tác nhỏ như copy link, xoá 1 item, gọi API của 1 card.

## Design tokens đề xuất
```css
:root {
  --cb-navy: #062b52;
  --cb-teal: #12a6a2;
  --cb-teal-dark: #07918e;
  --cb-bg: #f6fbfd;
  --cb-card: #ffffff;
  --cb-text: #062b52;
  --cb-muted: #7e8da0;
  --cb-border: #dce8ee;
  --cb-shadow: 0 10px 30px rgba(6,43,82,.08);
  --cb-radius-lg: 20px;
  --cb-radius-md: 14px;
}
```

## Component chuẩn nên tạo
Codex nên tạo các component/partial độc lập để dùng lại:
- `PageLoader`
- `InlineSpinner`
- `LoadingButton`
- `ContentLoadingOverlay`
- `SkeletonList`
- `LoadingModal`
- `ProgressLoader`

Nếu dự án là ASP.NET MVC/ABP MVC, có thể triển khai dưới dạng partial view + CSS class + JS helper. Không cần React.

## CSS loader cơ bản
```css
.cb-spinner {
  width: 28px;
  height: 28px;
  border: 4px solid rgba(18,166,162,.18);
  border-top-color: var(--cb-teal);
  border-radius: 50%;
  animation: cb-spin .8s linear infinite;
}
@keyframes cb-spin { to { transform: rotate(360deg); } }

.cb-loading-overlay {
  position: absolute;
  inset: 0;
  display: grid;
  place-items: center;
  background: rgba(255,255,255,.72);
  backdrop-filter: blur(2px);
  border-radius: inherit;
  z-index: 20;
}

.cb-skeleton {
  position: relative;
  overflow: hidden;
  background: #edf3f6;
  border-radius: 10px;
}
.cb-skeleton::after {
  content: '';
  position: absolute;
  inset: 0;
  transform: translateX(-100%);
  background: linear-gradient(90deg, transparent, rgba(255,255,255,.7), transparent);
  animation: cb-shimmer 1.2s infinite;
}
@keyframes cb-shimmer { 100% { transform: translateX(100%); } }
```

## JS helper đề xuất
```js
window.CatBackLoading = {
  setButtonLoading(button, loading, text = 'Đang xử lý...') {
    if (!button) return;
    if (loading) {
      button.dataset.oldHtml = button.innerHTML;
      button.disabled = true;
      button.innerHTML = `<span class="cb-spinner cb-spinner--sm"></span><span>${text}</span>`;
    } else {
      button.disabled = false;
      if (button.dataset.oldHtml) button.innerHTML = button.dataset.oldHtml;
    }
  },

  showOverlay(container) {
    if (!container || container.querySelector(':scope > .cb-loading-overlay')) return;
    container.style.position = container.style.position || 'relative';
    container.insertAdjacentHTML('beforeend',
      '<div class="cb-loading-overlay"><div class="cb-spinner"></div></div>');
  },

  hideOverlay(container) {
    container?.querySelector(':scope > .cb-loading-overlay')?.remove();
  }
};
```

## Flow đề xuất cho trang CatBack
### Chuyển trang
- Nếu route đổi và nội dung mới chưa sẵn sàng: page loader tối giản.
- Giữ header/bottom nav nếu app shell đã render; chỉ loading vùng nội dung để tránh cảm giác app bị reset.

### Tạo link hoàn tiền
- Khi bấm `Tạo link hoàn tiền`:
  1. Disable nút.
  2. Thay icon bằng spinner nhỏ.
  3. Text: `Đang tạo link...`.
  4. Không phủ toàn màn hình.
  5. Thành công: cập nhật card/list rồi trả nút về trạng thái thường.
  6. Lỗi: trả nút về trạng thái thường và hiển thị toast.

### Danh sách “Link của bạn”
- First load: hiển thị 4–5 skeleton rows có kích thước giống card thật.
- Reload/filter: ưu tiên overlay mờ trên danh sách hiện tại.

### Xoá link
- Chỉ loading trên nút xoá/card liên quan; các card khác vẫn thao tác được.

## Tiêu chí hoàn thành cho Codex
- Loader đúng màu CatBack, không dùng màu ngẫu nhiên.
- Animation mượt 60fps, spinner 0.7–0.9s/vòng.
- Không layout shift khi bắt đầu/kết thúc loading.
- Có `aria-busy`, `aria-live` hoặc text trạng thái cho accessibility.
- Disable double-click trong lúc submit.
- Có xử lý `try/finally` để luôn thoát loading khi API lỗi.
- Không hiển thị progress giả.
- Respect `prefers-reduced-motion`.
- Mobile-first, test tại 360px / 390px / 430px.

## Prompt ngắn để đưa thẳng cho Codex
```text
Dựa trên các asset PNG trong thư mục này và README.md, hãy triển khai bộ loading reusable cho dự án ASP.NET MVC/ABP MVC CatBack. Giữ nguyên header và bottom navigation hiện có. Tạo component/partial + CSS + JS helper cho: page loader, button loading, inline spinner, content overlay, skeleton list, modal processing và progress loader. Áp dụng đúng ngữ cảnh theo README, mobile-first, không gây layout shift, không dùng progress giả và đảm bảo mọi async action thoát loading trong finally. Ưu tiên skeleton cho danh sách Link của bạn và inline loading cho nút Tạo link hoàn tiền.
```

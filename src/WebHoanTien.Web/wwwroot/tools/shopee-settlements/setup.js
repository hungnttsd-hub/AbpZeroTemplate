(() => {
  const button = document.getElementById("copy");
  const code = document.getElementById("code");
  const status = document.getElementById("status");
  const verify = document.getElementById("verify");
  button.addEventListener("click", async () => {
    try {
      await navigator.clipboard.writeText(code.value);
      status.textContent = "Đã sao chép mã tải ngắn. Thay toàn bộ URL cũ của dấu trang CatsBack JSON rồi lưu lại.";
    } catch (_) {
      document.getElementById("manual").open = true;
      code.focus();
      code.select();
      code.setSelectionRange(0, code.value.length);
      status.textContent = "Hãy nhấn giữ vùng mã, chọn tất cả rồi sao chép và dán vào URL của dấu trang.";
    }
  });
  verify.addEventListener("click", () => {
    const saved = document.getElementById("saved-code").value.trim();
    const result = document.getElementById("verify-result");
    if (!saved.startsWith("javascript:")) {
      result.textContent = "URL chưa đúng: phải bắt đầu bằng javascript:, không phải địa chỉ trang hướng dẫn. Hãy thay toàn bộ URL của dấu trang bằng mã vừa sao chép.";
      return;
    }
    try {
      const decoded = decodeURIComponent(code.value);
      if (saved === code.value || saved === decoded || decodeURIComponent(saved) === decoded) {
        result.textContent = "Mã tải ngắn đã lưu chính xác. Quay lại tab Shopee, chạy CatsBack JSON; nếu có thông báo lỗi, gửi lại nguyên nội dung đó.";
      } else {
        result.textContent = "Mã đã lưu không khớp: có thể còn mã cũ hoặc bị thiếu khi sao chép/lưu. Hãy sao chép mã tải ngắn và thay toàn bộ URL dấu trang, lưu rồi kiểm tra lại.";
      }
    } catch (_) {
      result.textContent = "URL chứa phần mã hóa không hợp lệ. Hãy sao chép lại mã tải ngắn vào URL dấu trang.";
    }
  });
  if (location.protocol !== "https:") {
    status.textContent = "Mở trang cài đặt này trên website CatsBack qua HTTPS để tạo mã tải tool dùng được từ Shopee.";
    return;
  }
  fetch("loader-template.txt?v=3", { cache: "no-store" })
    .then(response => {
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      return response.text();
    })
    .then(source => {
      const placeholder = '"__CATSBACK_MOBILE_URL__"';
      if (!source.startsWith("/*catsback-loader-v3*/") || source.split(placeholder).length !== 2) {
        throw new Error("Mẫu mã tải tool không hợp lệ.");
      }
      const toolUrl = new URL("mobile.js", location.href).href;
      // Only a short script loader is stored in the bookmark, not the collector.
      code.value = `javascript:${encodeURIComponent(source.trim().replace(placeholder, JSON.stringify(toolUrl)))}`;
      button.disabled = false;
      verify.disabled = false;
      status.textContent = `Mã tải ngắn đã sẵn sàng (${code.value.length.toLocaleString("vi-VN")} ký tự). Thay URL dấu trang cũ một lần; những lần sau tool được tải từ website.`;
    })
    .catch(error => {
      status.textContent = `Không tải được mã tool: ${error.message}. Hãy tải lại trang hoặc kiểm tra bản triển khai website.`;
    });
})();

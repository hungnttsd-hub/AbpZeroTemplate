(() => {
  const button = document.getElementById("copy");
  const code = document.getElementById("code");
  const status = document.getElementById("status");
  const verify = document.getElementById("verify");
  button.addEventListener("click", async () => {
    try {
      await navigator.clipboard.writeText(code.value);
      status.textContent = "Đã sao chép mã v2. Thay toàn bộ URL cũ của dấu trang CatsBack JSON rồi lưu lại.";
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
        result.textContent = "Mã đã lưu khớp bản v2 và không bị thiếu. Quay lại tab Shopee, chạy CatsBack JSON; nếu có thông báo lỗi, gửi lại nguyên nội dung đó.";
      } else {
        result.textContent = "Mã đã lưu không khớp bản v2: có thể còn mã cũ hoặc bị thiếu khi sao chép/lưu. Hãy sao chép mã v2 và thay toàn bộ URL dấu trang, lưu rồi kiểm tra lại.";
      }
    } catch (_) {
      result.textContent = "URL chứa phần mã hóa không hợp lệ. Hãy sao chép lại toàn bộ mã v2 vào URL dấu trang.";
    }
  });
  fetch("catsback-json-bookmarklet.txt?v=2", { cache: "no-store" })
    .then(response => {
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      return response.text();
    })
    .then(source => {
      const bookmarklet = source.trim();
      if (!bookmarklet.startsWith("javascript:") ||
          !bookmarklet.endsWith(encodeURIComponent("/*catsback-mobile-v2-end*/"))) {
        throw new Error("Mã dấu trang v2 chưa đầy đủ.");
      }
      // Already minified at build time; no eval or cross-origin script loader.
      code.value = bookmarklet;
      button.disabled = false;
      verify.disabled = false;
      status.textContent = "Mã v2 đã sẵn sàng. Nếu đã cài bản trước, cần thay URL dấu trang bằng mã mới này.";
    })
    .catch(error => {
      status.textContent = `Không tải được mã tool: ${error.message}. Hãy tải lại trang hoặc kiểm tra bản triển khai website.`;
    });
})();

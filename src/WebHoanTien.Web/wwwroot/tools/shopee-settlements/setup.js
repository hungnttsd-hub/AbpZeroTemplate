(() => {
  const button = document.getElementById("copy");
  const code = document.getElementById("code");
  const status = document.getElementById("status");
  button.addEventListener("click", async () => {
    try {
      await navigator.clipboard.writeText(code.value);
      status.textContent = "Đã sao chép. Dán vào ô URL khi chỉnh sửa dấu trang CatsBack JSON.";
    } catch (_) {
      document.getElementById("manual").open = true;
      code.focus();
      code.select();
      code.setSelectionRange(0, code.value.length);
      status.textContent = "Hãy nhấn giữ vùng mã, chọn tất cả rồi sao chép và dán vào URL của dấu trang.";
    }
  });
  fetch("bookmarklet.js", { cache: "no-store" })
    .then(response => {
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      return response.text();
    })
    .then(source => {
      if (!source.includes("collectShopeeSettlementRowsInPage")) throw new Error("Thiếu mã tổng hợp.");
      // Embed the complete script: no cross-origin script loading or eval in Shopee.
      code.value = `javascript:${encodeURIComponent(`void function(){\n${source}\n}()`)}`;
      button.disabled = false;
      status.textContent = "Mã đã sẵn sàng. Chỉ cần tạo dấu trang một lần trên điện thoại.";
    })
    .catch(error => {
      status.textContent = `Không tải được mã tool: ${error.message}. Hãy tải lại trang hoặc kiểm tra bản triển khai website.`;
    });
})();

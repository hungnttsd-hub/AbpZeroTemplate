function loadCatsBack(url) {
  if (location.origin !== "https://affiliate.shopee.vn") return alert("Mở trang Shopee Affiliate trước.");
  const panel = document.getElementById("catsback-mobile-settlements");
  if (panel) return void panel.dispatchEvent(new Event("catsback:show"));
  if (document.getElementById("catsback-loader")) return;
  const script = document.createElement("script");
  script.id = "catsback-loader";
  let timer;
  const clean = () => { clearTimeout(timer); script.remove(); };
  const fail = () => {
    clean();
    alert("Không tải được CatsBack. Kiểm tra mạng; trang Shopee cũng có thể đang chặn tải tool.");
  };
  try {
    script.src = url + "?t=" + Date.now();
    script.referrerPolicy = "no-referrer";
    script.onerror = fail;
    script.onload = () => {
      clean();
      if (!document.getElementById("catsback-mobile-settlements")) alert("Đã tải tool nhưng chưa mở được. Gửi lại thông báo lỗi cho CatsBack.");
    };
    timer = setTimeout(fail, 15000);
    document.documentElement.append(script);
  } catch (error) {
    clean();
    alert("CatsBack: " + error.message);
  }
}

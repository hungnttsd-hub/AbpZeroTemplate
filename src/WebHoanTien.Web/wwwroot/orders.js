(function () {
  async function copyText(value) {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(value);
      return;
    }

    const input = document.createElement("textarea");
    input.value = value;
    input.setAttribute("readonly", "");
    input.style.position = "fixed";
    input.style.opacity = "0";
    document.body.appendChild(input);
    input.select();
    document.execCommand("copy");
    input.remove();
  }

  document.addEventListener("click", async function (event) {
    const button = event.target.closest("[data-copy-order-code]");
    if (!button) {
      return;
    }

    const orderCode = button.dataset.copyOrderCode;
    if (!orderCode) {
      return;
    }

    try {
      await copyText(orderCode);
      button.classList.add("is-copied");
      button.setAttribute("aria-label", "Đã sao chép mã đơn hàng");
      window.setTimeout(function () {
        button.classList.remove("is-copied");
      }, 1400);
    } catch {
      button.classList.remove("is-copied");
    }
  });
})();

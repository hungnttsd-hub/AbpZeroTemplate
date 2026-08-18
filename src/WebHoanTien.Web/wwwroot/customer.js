(() => {
  const input = document.getElementById('affiliate-url');
  const validate = document.getElementById('validate-button');
  const create = document.getElementById('create-button');
  const status = document.getElementById('url-status');
  if (!input || !validate || !create || !status) return;

  const setStatus = (message, state) => {
    status.textContent = message;
    status.dataset.state = state;
  };

  input.addEventListener('input', () => {
    create.disabled = true;
    setStatus('Bấm “Kiểm tra link” trước khi tạo link mua hàng.', 'idle');
  });

  validate.addEventListener('click', async () => {
    create.disabled = true;
    validate.disabled = true;
    setStatus('Đang kiểm tra định dạng và tên miền…', 'loading');
    try {
      const requestVerificationToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
      const response = await fetch('/api/app/affiliate-links/validate', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(requestVerificationToken ? { RequestVerificationToken: requestVerificationToken } : {})
        },
        body: JSON.stringify({ url: input.value })
      });
      const body = await response.json();
      if (response.ok && body.isValid) {
        create.disabled = false;
        setStatus(body.requiresRedirectResolution ? 'Link rút gọn hợp lệ. Redirect sẽ được kiểm tra an toàn khi tạo link.' : 'Link Shopee hợp lệ — bạn có thể tiếp tục.', 'success');
      } else setStatus(body.error || 'Link không hợp lệ.', 'error');
    } catch { setStatus('Không thể kiểm tra lúc này. Vui lòng thử lại.', 'error'); }
    finally { validate.disabled = false; }
  });
})();

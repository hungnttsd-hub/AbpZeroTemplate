(() => {
  const root = document.querySelector('[data-google-identity]');
  if (!root) {
    return;
  }

  const clientId = root.dataset.clientId;
  const form = root.querySelector('[data-google-identity-form]');
  const button = root.querySelector('[data-google-identity-button]');
  const status = root.querySelector('[data-google-identity-status]');
  let codeClient;
  let submitting = false;

  if (!clientId || !form || !button || !status) {
    return;
  }

  button.disabled = true;

  const showStatus = (message, state = 'error') => {
    status.hidden = false;
    status.dataset.state = state;
    status.textContent = message;
  };

  const readPayload = async (response) => {
    const contentType = response.headers.get('content-type') || '';
    return contentType.includes('application/json') ? response.json() : {};
  };

  const handleCode = async (googleResponse) => {
    if (submitting || !googleResponse?.code) {
      if (googleResponse?.error) {
        showStatus(googleResponse.error_description || 'Google không thể hoàn tất đăng nhập.');
      }
      return;
    }

    submitting = true;
    button.disabled = true;
    button.classList.add('is-loading');
    showStatus('Đang xác minh tài khoản Google…', 'loading');

    const body = new FormData(form);
    body.set('code', googleResponse.code);
    body.set('redirectUri', window.location.origin);
    body.set('acceptedTerms', 'true');
    body.set('returnUrl', root.dataset.returnUrl || '/');

    try {
      const response = await fetch(form.action, {
        method: 'POST',
        body,
        credentials: 'same-origin',
        headers: {
          Accept: 'application/json',
          'X-Requested-With': 'XMLHttpRequest'
        }
      });
      const payload = await readPayload(response);

      if (!response.ok) {
        throw new Error(payload.message || 'Không thể đăng nhập bằng Google lúc này.');
      }

      window.location.assign(payload.redirectUrl || '/');
    } catch (error) {
      showStatus(error instanceof Error ? error.message : 'Không thể đăng nhập bằng Google lúc này.');
      submitting = false;
      button.disabled = false;
      button.classList.remove('is-loading');
    }
  };

  const initializeGoogleIdentity = () => {
    if (!window.google?.accounts?.oauth2) {
      showStatus('Không thể tải dịch vụ đăng nhập Google. Vui lòng kiểm tra kết nối và thử lại.');
      return;
    }

    codeClient = window.google.accounts.oauth2.initCodeClient({
      client_id: clientId,
      scope: 'openid email profile',
      ux_mode: 'popup',
      select_account: true,
      callback: handleCode,
      error_callback: (error) => {
        if (error?.type !== 'popup_closed') {
          showStatus('Không thể mở cửa sổ đăng nhập Google. Vui lòng thử lại.');
        }
      }
    });
    button.disabled = false;
    button.addEventListener('click', () => {
      status.hidden = true;
      codeClient.requestCode();
    });
  };

  const script = document.createElement('script');
  script.src = 'https://accounts.google.com/gsi/client';
  script.async = true;
  script.defer = true;
  script.onload = initializeGoogleIdentity;
  script.onerror = () => showStatus('Không thể tải dịch vụ đăng nhập Google. Vui lòng thử lại sau.');
  document.head.appendChild(script);
})();

(() => {
  const root = document.querySelector('[data-google-identity]');
  if (!root) {
    return;
  }

  const clientId = root.dataset.clientId;
  const form = root.querySelector('[data-google-identity-form]');
  const button = root.querySelector('[data-google-identity-button]');
  const status = root.querySelector('[data-google-identity-status]');
  let submitting = false;

  if (!clientId || !form || !button || !status) {
    return;
  }

  const showStatus = (message, state = 'error') => {
    status.hidden = false;
    status.dataset.state = state;
    status.textContent = message;
  };

  const readPayload = async (response) => {
    const contentType = response.headers.get('content-type') || '';
    return contentType.includes('application/json') ? response.json() : {};
  };

  const handleCredential = async (googleResponse) => {
    if (submitting || !googleResponse?.credential) {
      return;
    }

    submitting = true;
    button.classList.add('is-loading');
    showStatus('Đang xác minh tài khoản Google…', 'loading');

    const body = new FormData(form);
    body.set('credential', googleResponse.credential);
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
      button.classList.remove('is-loading');
    }
  };

  const initializeGoogleIdentity = () => {
    if (!window.google?.accounts?.id) {
      showStatus('Không thể tải dịch vụ đăng nhập Google. Vui lòng kiểm tra kết nối và thử lại.');
      return;
    }

    window.google.accounts.id.disableAutoSelect();
    window.google.accounts.id.initialize({
      client_id: clientId,
      callback: handleCredential,
      ux_mode: 'popup',
      use_fedcm_for_button: false,
      auto_select: false
    });
    const availableWidth = Math.floor(root.getBoundingClientRect().width);
    window.google.accounts.id.renderButton(button, {
      type: 'standard',
      theme: 'outline',
      size: 'large',
      text: 'continue_with',
      shape: 'rectangular',
      logo_alignment: 'left',
      width: Math.min(400, Math.max(200, availableWidth))
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

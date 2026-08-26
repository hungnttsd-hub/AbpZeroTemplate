(() => {
  const installButton = document.getElementById('install-app-button');
  const userAgent = window.navigator.userAgent || '';
  const isIos = /iphone|ipad|ipod/i.test(userAgent)
    || (window.navigator.platform === 'MacIntel' && window.navigator.maxTouchPoints > 1);
  const isAndroid = /android/i.test(userAgent);
  const isIosSafari = isIos
    && /safari/i.test(userAgent)
    && !/crios|fxios|edgios|opios/i.test(userAgent);
  const isInAppBrowser = /FBAN|FBAV|Instagram|Messenger|Zalo|ZBrowser|Line|TikTok/i.test(userAgent);
  const isChromeAndroid = isAndroid
    && /chrome/i.test(userAgent)
    && !/edga|opr|samsungbrowser/i.test(userAgent);
  const isStandalone = () => window.matchMedia('(display-mode: standalone)').matches
    || window.navigator.standalone === true;
  const isMobile = () => window.matchMedia('(max-width: 820px)').matches
    || window.navigator.userAgentData?.mobile === true;
  let deferredInstallPrompt;

  if ('serviceWorker' in window.navigator && window.isSecureContext) {
    window.navigator.serviceWorker.register('/service-worker.js', { scope: '/' }).catch(() => undefined);
  }

  if (!installButton) return;

  const hideInstallButton = () => {
    installButton.hidden = true;
    installButton.disabled = false;
  };

  const showInstallButton = () => {
    installButton.hidden = false;
    installButton.disabled = false;
    installButton.textContent = 'Cài đặt ngay';
    installButton.setAttribute('aria-label', 'Cài đặt CatsBack vào màn hình chính');
  };

  const showHelp = (message, title = 'Cài đặt CatsBack') => {
    if (window.CatsBackModal?.info) {
      window.CatsBackModal.info({
        title,
        message,
        confirmText: 'Đã hiểu'
      });
      return;
    }

    window.alert(`${title}\n\n${message}`);
  };

  const refreshInstallButton = () => {
    if (isStandalone()) {
      hideInstallButton();
      return;
    }

    if (isMobile() || isIos || isInAppBrowser || deferredInstallPrompt) {
      showInstallButton();
      return;
    }

    hideInstallButton();
  };

  const showInAppBrowserHelp = () => {
    if (isIos) {
      showHelp('Zalo/Facebook không hỗ trợ cài ứng dụng trực tiếp.\n\n1. Mở menu của trình duyệt hiện tại và chọn “Mở bằng Safari”.\n2. Trong Safari, bấm nút Chia sẻ.\n3. Chọn “Thêm vào Màn hình chính”, sau đó bấm “Thêm”.', 'Mở CatsBack bằng Safari');
      return;
    }

    showHelp('Zalo/Facebook không hỗ trợ cài ứng dụng trực tiếp.\n\n1. Bấm menu ⋮ của trình duyệt hiện tại.\n2. Chọn “Mở bằng trình duyệt” hoặc “Mở bằng Chrome”.\n3. Trong Chrome, quay lại bấm “Cài đặt ngay”.', 'Mở CatsBack bằng Chrome');
  };

  const showManualInstallHelp = () => {
    if (isInAppBrowser) {
      showInAppBrowserHelp();
      return;
    }

    if (isIosSafari) {
      showHelp('1. Bấm nút Chia sẻ trên thanh công cụ Safari.\n2. Kéo xuống và chọn “Thêm vào Màn hình chính”.\n3. Bấm “Thêm” để hoàn tất.');
      return;
    }

    if (isIos) {
      showHelp('iPhone chỉ hỗ trợ thêm CatsBack từ Safari.\n\nHãy mở trang này bằng Safari, bấm Chia sẻ → “Thêm vào Màn hình chính” → “Thêm”.', 'Mở CatsBack bằng Safari');
      return;
    }

    if (isChromeAndroid) {
      showHelp('Nếu hộp cài đặt chưa xuất hiện, bấm menu ⋮ ở góc trên bên phải của Chrome rồi chọn “Cài đặt ứng dụng” hoặc “Thêm vào Màn hình chính”.');
      return;
    }

    showHelp('Mở trang này bằng Chrome trên Android hoặc Safari trên iPhone, sau đó chọn “Cài đặt ứng dụng” hoặc “Thêm vào Màn hình chính”.');
  };

  window.addEventListener('beforeinstallprompt', (event) => {
    event.preventDefault();
    deferredInstallPrompt = event;
    showInstallButton();
  });

  window.addEventListener('appinstalled', () => {
    deferredInstallPrompt = undefined;
    hideInstallButton();
  });

  window.addEventListener('pageshow', () => {
    refreshInstallButton();
  });

  window.addEventListener('focus', () => {
    refreshInstallButton();
  });

  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') refreshInstallButton();
  });

  const standaloneMediaQuery = window.matchMedia('(display-mode: standalone)');
  if (typeof standaloneMediaQuery.addEventListener === 'function') {
    standaloneMediaQuery.addEventListener('change', refreshInstallButton);
  }

  installButton.addEventListener('click', async () => {
    if (isStandalone()) {
      hideInstallButton();
      return;
    }

    if (isInAppBrowser) {
      showInAppBrowserHelp();
      return;
    }

    if (!deferredInstallPrompt) {
      showManualInstallHelp();
      return;
    }

    const installPrompt = deferredInstallPrompt;
    deferredInstallPrompt = undefined;

    try {
      await installPrompt.prompt();
      const choice = await installPrompt.userChoice;
      if (choice.outcome === 'accepted') {
        hideInstallButton();
      } else {
        showInstallButton();
        showManualInstallHelp();
      }
    } catch {
      showInstallButton();
      showManualInstallHelp();
    }
  });

  refreshInstallButton();
})();

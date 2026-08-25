(() => {
  const installButton = document.getElementById('install-app-button');
  const help = document.getElementById('pwa-install-help');
  const helpMessage = document.getElementById('pwa-install-message');
  const closeHelpButton = document.getElementById('pwa-install-close');
  const isIos = /iphone|ipad|ipod/i.test(window.navigator.userAgent)
    || (window.navigator.platform === 'MacIntel' && window.navigator.maxTouchPoints > 1);
  const isIosSafari = isIos && /safari/i.test(window.navigator.userAgent)
    && !/crios|fxios|edgios|opios/i.test(window.navigator.userAgent);
  const isStandalone = () => window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
  const isMobile = () => window.matchMedia('(max-width: 820px)').matches;
  const installedStorageKey = 'catback-shortcut-installed';
  let deferredInstallPrompt;

  const markShortcutAsInstalled = () => {
    try {
      window.localStorage.setItem(installedStorageKey, 'true');
    } catch { }
  };

  const clearInstalledShortcutFlag = () => {
    try {
      window.localStorage.removeItem(installedStorageKey);
    } catch { }
  };

  const isShortcutKnownInstalled = () => {
    try {
      return window.localStorage.getItem(installedStorageKey) === 'true';
    } catch {
      return false;
    }
  };

  const hideInstallButton = () => {
    installButton.hidden = true;
    installButton.disabled = false;
    installButton.classList.remove('is-installed');
  };

  const showAvailableInstallState = () => {
    installButton.hidden = false;
    installButton.disabled = false;
    installButton.classList.remove('is-installed');
    installButton.textContent = 'Thêm lối tắt';
    installButton.removeAttribute('aria-label');
  };

  if ('serviceWorker' in navigator && window.isSecureContext) {
    navigator.serviceWorker.register('/service-worker.js', { scope: '/' }).catch(() => undefined);
  }

  const showHelp = (message) => {
    helpMessage.textContent = message;
    help.hidden = false;
    help.focus();
  };

  const showInstallButton = () => {
    if (!isMobile() && !isIos) return;
    if (isStandalone()) {
      markShortcutAsInstalled();
      hideInstallButton();
      return;
    }

    if (isShortcutKnownInstalled()) {
      hideInstallButton();
      return;
    }

    showAvailableInstallState();
  };

  const showManualInstallHelp = () => {
    showHelp(isIosSafari
      ? 'Trên Safari:\n1. Bấm nút Chia sẻ ở thanh công cụ.\n2. Kéo xuống chọn “Thêm vào Màn hình chính”.\n3. Bật “Mở dưới dạng ứng dụng web”, rồi bấm “Thêm”.'
      : isIos
        ? 'Để thêm CatBack trên iPhone, hãy mở trang này bằng Safari. Sau đó bấm Chia sẻ → “Thêm vào Màn hình chính” → bật “Mở dưới dạng ứng dụng web” → “Thêm”.'
      : 'Nếu hộp cài đặt chưa hiện, trong Chrome bấm ⋮ ở góc trên bên phải rồi chọn “Cài đặt ứng dụng” hoặc “Thêm vào Màn hình chính”.');
  };

  window.addEventListener('beforeinstallprompt', (event) => {
    event.preventDefault();
    deferredInstallPrompt = event;
    clearInstalledShortcutFlag();
    showAvailableInstallState();
  });

  window.addEventListener('appinstalled', () => {
    deferredInstallPrompt = undefined;
    markShortcutAsInstalled();
    hideInstallButton();
  });

  installButton.addEventListener('click', async () => {
    if (!deferredInstallPrompt) {
      showManualInstallHelp();
      return;
    }

    const installPrompt = deferredInstallPrompt;
    deferredInstallPrompt = undefined;

    try {
      await installPrompt.prompt();
      const choice = await installPrompt.userChoice;
      if (choice.outcome !== 'accepted') showManualInstallHelp();
    } catch {
      showManualInstallHelp();
    }
  });

  closeHelpButton.addEventListener('click', () => { help.hidden = true; });
  help.addEventListener('click', (event) => {
    if (event.target === help) help.hidden = true;
  });

  showInstallButton();
})();

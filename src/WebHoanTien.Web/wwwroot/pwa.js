(() => {
  const installButton = document.getElementById('install-app-button');
  const userAgent = window.navigator.userAgent || '';
  const isIos = /iphone|ipad|ipod/i.test(userAgent)
    || (window.navigator.platform === 'MacIntel' && window.navigator.maxTouchPoints > 1);
  const isAndroid = /android/i.test(userAgent);
  const isIosChrome = isIos && /crios/i.test(userAgent);
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
    || (window.navigator.userAgentData && window.navigator.userAgentData.mobile === true);
  let deferredInstallPrompt;
  let installHelp;

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
    installButton.setAttribute(
      'aria-label',
      isIos ? 'Xem hướng dẫn thêm CatsBack vào màn hình chính iPhone' : 'Cài đặt CatsBack vào màn hình chính'
    );
  };

  const ensureInstallHelp = () => {
    if (installHelp) return installHelp;

    const root = document.createElement('div');
    root.className = 'pwa-install-help';
    root.hidden = true;
    root.setAttribute('aria-hidden', 'true');

    const card = document.createElement('section');
    card.className = 'pwa-install-card';
    card.setAttribute('role', 'dialog');
    card.setAttribute('aria-modal', 'true');
    card.setAttribute('aria-labelledby', 'pwa-install-title');

    const closeButton = document.createElement('button');
    closeButton.className = 'pwa-install-close';
    closeButton.type = 'button';
    closeButton.setAttribute('aria-label', 'Đóng hướng dẫn cài đặt');
    closeButton.textContent = '×';

    const icon = document.createElement('span');
    icon.className = 'pwa-install-icon';
    icon.setAttribute('aria-hidden', 'true');
    icon.textContent = '↑';

    const title = document.createElement('h2');
    title.id = 'pwa-install-title';

    const intro = document.createElement('p');
    intro.className = 'pwa-install-intro';

    const steps = document.createElement('ol');
    steps.className = 'pwa-install-steps';

    const note = document.createElement('p');
    note.className = 'pwa-install-note';

    const confirmButton = document.createElement('button');
    confirmButton.className = 'pwa-install-confirm';
    confirmButton.type = 'button';
    confirmButton.textContent = 'Đã hiểu';

    card.append(closeButton, icon, title, intro, steps, note, confirmButton);
    root.appendChild(card);
    document.body.appendChild(root);

    const close = () => {
      root.hidden = true;
      root.setAttribute('aria-hidden', 'true');
      document.body.classList.remove('pwa-install-open');
      installButton.focus();
    };

    closeButton.addEventListener('click', close);
    confirmButton.addEventListener('click', close);
    root.addEventListener('click', (event) => {
      if (event.target === root) close();
    });
    document.addEventListener('keydown', (event) => {
      if (!root.hidden && event.key === 'Escape') close();
    });

    installHelp = { root, title, intro, steps, note, confirmButton };
    return installHelp;
  };

  const showInstallGuide = ({ title, intro, steps, note }) => {
    const help = ensureInstallHelp();
    help.title.textContent = title;
    help.intro.textContent = intro;
    while (help.steps.firstChild) help.steps.removeChild(help.steps.firstChild);
    steps.forEach((step) => {
      const item = document.createElement('li');
      item.textContent = step;
      help.steps.appendChild(item);
    });
    help.note.textContent = note || '';
    help.note.hidden = !note;
    help.root.hidden = false;
    help.root.setAttribute('aria-hidden', 'false');
    document.body.classList.add('pwa-install-open');
    window.requestAnimationFrame(() => help.confirmButton.focus());
  };

  const showHelp = (message, title = 'Cài đặt CatsBack') => {
    if (window.CatsBackModal && window.CatsBackModal.info) {
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
      showInstallGuide({
        title: 'Mở CatsBack bằng Safari',
        intro: 'Zalo, Facebook và Messenger không thể thêm lối tắt trực tiếp trên iPhone.',
        steps: [
          'Mở menu của trình duyệt hiện tại và chọn “Mở bằng Safari”.',
          'Trong Safari, bấm nút Chia sẻ (hình vuông có mũi tên hướng lên).',
          'Chọn “Thêm vào Màn hình chính”.',
          'Bật “Mở dưới dạng ứng dụng web” nếu iPhone hiển thị tùy chọn này, sau đó bấm “Thêm”.'
        ],
        note: 'Sau khi thêm, hãy trở về Màn hình chính và tìm biểu tượng CatsBack. Safari không tự mở ứng dụng sau khi hoàn tất.'
      });
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
      showInstallGuide({
        title: 'Thêm CatsBack vào iPhone',
        intro: 'iPhone không mở hộp cài đặt tự động. Bạn thực hiện lần lượt các bước sau:',
        steps: [
          'Bấm nút Chia sẻ trong Safari (hình vuông có mũi tên hướng lên).',
          'Kéo xuống và chọn “Thêm vào Màn hình chính”.',
          'Bật “Mở dưới dạng ứng dụng web” nếu iPhone hiển thị tùy chọn này.',
          'Kiểm tra tên CatsBack rồi bấm “Thêm” để hoàn tất.'
        ],
        note: 'Sau khi thêm, hãy trở về Màn hình chính và tìm biểu tượng CatsBack. Safari không tự mở ứng dụng hoặc tự ẩn nút này.'
      });
      return;
    }

    if (isIosChrome) {
      showInstallGuide({
        title: 'Cài CatsBack bằng Safari',
        intro: 'Để tránh lỗi thêm lối tắt trên Chrome iPhone, hãy thực hiện bằng Safari.',
        steps: [
          'Sao chép địa chỉ CatsBack và mở địa chỉ đó trong Safari.',
          'Bấm Chia sẻ rồi chọn “Thêm vào Màn hình chính”.',
          'Bật “Mở dưới dạng ứng dụng web” nếu iPhone hiển thị tùy chọn này.',
          'Bấm “Thêm”, sau đó trở về Màn hình chính để mở CatsBack.'
        ],
        note: 'Việc thêm ứng dụng web do iPhone xử lý; website không thể tự bấm nút Thêm thay người dùng.'
      });
      return;
    }

    if (isIos) {
      showInstallGuide({
        title: 'Thêm CatsBack vào iPhone',
        intro: 'Trình duyệt hiện tại không cung cấp hộp cài đặt tự động.',
        steps: [
          'Mở trang này bằng Safari.',
          'Bấm nút Chia sẻ (hình vuông có mũi tên hướng lên).',
          'Chọn “Thêm vào Màn hình chính” và bật “Mở dưới dạng ứng dụng web” nếu có.',
          'Bấm “Thêm”, sau đó trở về Màn hình chính để mở CatsBack.'
        ],
        note: 'Đây là cách cài ứng dụng web do iPhone hỗ trợ.'
      });
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

(() => {
  const root = document.querySelector('[data-guide-root]');
  if (!root) return;

  const userAgent = window.navigator.userAgent || window.navigator.vendor || '';
  const isIos = /iPad|iPhone|iPod/i.test(userAgent)
    || (window.navigator.platform === 'MacIntel' && window.navigator.maxTouchPoints > 1);
  const platform = isIos ? 'ios' : /android/i.test(userAgent) ? 'android' : 'unknown';
  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  document.querySelectorAll('[data-platform-text]').forEach((element) => {
    const text = element.dataset[platform] || element.dataset.unknown;
    if (text) element.textContent = text;
  });

  document.querySelectorAll('[data-platform-badge]').forEach((element) => {
    element.textContent = platform === 'android' ? 'A' : platform === 'ios' ? 'iOS' : '•';
  });

  const accordion = document.querySelector('[data-guide-accordion]');
  if (accordion) {
    const buttons = Array.from(accordion.querySelectorAll('button[aria-controls]'));

    const closePanel = (button, panel) => {
      button.setAttribute('aria-expanded', 'false');
      panel.classList.add('is-collapsed');
      window.setTimeout(() => {
        if (button.getAttribute('aria-expanded') === 'false') panel.hidden = true;
      }, reduceMotion ? 0 : 210);
    };

    const openPanel = (button, panel) => {
      panel.hidden = false;
      panel.classList.add('is-collapsed');
      window.requestAnimationFrame(() => {
        button.setAttribute('aria-expanded', 'true');
        panel.classList.remove('is-collapsed');
      });
    };

    buttons.forEach((button) => {
      const panel = document.getElementById(button.getAttribute('aria-controls'));
      if (!panel) return;

      button.addEventListener('click', () => {
        const isOpen = button.getAttribute('aria-expanded') === 'true';
        buttons.forEach((otherButton) => {
          if (otherButton === button || otherButton.getAttribute('aria-expanded') !== 'true') return;
          const otherPanel = document.getElementById(otherButton.getAttribute('aria-controls'));
          if (otherPanel) closePanel(otherButton, otherPanel);
        });

        if (isOpen) closePanel(button, panel);
        else openPanel(button, panel);
      });
    });
  }

  const hash = window.location.hash;
  if (hash) {
    const targetId = hash.slice(1);
    const validTargets = new Set(['cai-dat', 'tao-link-hoan-tien', 'dang-ky', 'luu-y-hoa-hong']);
    const target = validTargets.has(targetId) ? document.getElementById(targetId) : null;
    if (target) {
      window.requestAnimationFrame(() => {
        window.setTimeout(() => {
          target.scrollIntoView({ behavior: reduceMotion ? 'auto' : 'smooth', block: 'center' });
          target.classList.add('is-guide-target');
          window.setTimeout(() => target.classList.remove('is-guide-target'), reduceMotion ? 0 : 1000);
        }, 60);
      });
    }
  }

  document.querySelectorAll('[data-guide-player]').forEach((playerButton) => {
    playerButton.addEventListener('click', () => {
      const videoId = playerButton.dataset[`video${platform[0].toUpperCase()}${platform.slice(1)}`]
        || playerButton.dataset.videoUnknown;
      if (!videoId) return;

      const player = document.createElement('div');
      player.className = `${playerButton.className} is-playing`;

      const iframe = document.createElement('iframe');
      const origin = encodeURIComponent(window.location.origin);
      iframe.src = `https://www.youtube.com/embed/${encodeURIComponent(videoId)}?autoplay=1&playsinline=1&rel=0&hl=vi&origin=${origin}`;
      iframe.title = playerButton.dataset.videoTitle || 'Video hướng dẫn CatBack';
      iframe.allow = 'accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share';
      iframe.allowFullscreen = true;
      iframe.referrerPolicy = 'strict-origin-when-cross-origin';
      player.appendChild(iframe);
      playerButton.replaceWith(player);
    }, { once: true });
  });
})();

(() => {
  'use strict';

  const pendingClass = 'cb-pwa-launch-pending';
  const storageKey = 'catback:pwa-launch-pending';
  const shownKey = 'catback:pwa-launch-shown:v1';
  const root = document.querySelector('[data-cb-launch-screen]');
  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  if (!root || !document.documentElement.classList.contains(pendingClass)) {
    document.documentElement.classList.remove(pendingClass);
    return;
  }

  try {
    window.sessionStorage.removeItem(storageKey);
    window.sessionStorage.setItem(shownKey, '1');
  } catch {
    // Storage may be unavailable in a restricted browser context.
  }

  let finished = false;
  const finish = () => {
    if (finished) return;
    finished = true;
    root.classList.add('is-leaving');

    window.setTimeout(() => {
      root.remove();
      document.documentElement.classList.remove(pendingClass);
    }, reduceMotion ? 0 : 450);
  };

  const finishAfterFirstPaint = () => {
    window.requestAnimationFrame(() => {
      window.requestAnimationFrame(() => window.setTimeout(finish, reduceMotion ? 0 : 420));
    });
  };

  const artworkReady = Promise.all(Array.from(root.querySelectorAll('img')).map((image) => {
    if (typeof image.decode !== 'function') return Promise.resolve();
    return image.decode().catch(() => undefined);
  }));
  const artworkTimeout = new Promise((resolve) => window.setTimeout(resolve, 500));

  Promise.race([artworkReady, artworkTimeout]).then(finishAfterFirstPaint);

  document.addEventListener('turbo:before-cache', finish, { once: true });
  document.addEventListener('turbo:before-render', finish, { once: true });
  window.setTimeout(finish, 3200);
})();

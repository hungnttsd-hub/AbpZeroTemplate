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
  let removalTimer;
  const removeLaunchScreen = () => {
    finished = true;
    window.clearTimeout(removalTimer);
    root.remove();
    document.documentElement.classList.remove(pendingClass);
  };

  const finish = () => {
    if (finished) return;
    finished = true;
    root.classList.add('is-leaving');

    removalTimer = window.setTimeout(removeLaunchScreen, reduceMotion ? 0 : 450);
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

  // Remove synchronously before a snapshot or body replacement; an exit timer
  // could otherwise leave a pending splash in the restored page.
  document.addEventListener('turbo:before-cache', removeLaunchScreen, { once: true });
  document.addEventListener('turbo:before-render', removeLaunchScreen, { once: true });
  window.addEventListener('pagehide', removeLaunchScreen, { once: true });
  window.addEventListener('pageshow', (event) => {
    if (event.persisted) removeLaunchScreen();
  });
  root.addEventListener('animationend', (event) => {
    if (event.target === root && event.animationName === 'cb-launch-safety-exit') {
      removeLaunchScreen();
    }
  });
  window.setTimeout(finish, 3200);
})();

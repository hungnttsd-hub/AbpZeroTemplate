(() => {
  if (window.__catBackNavigationInitialized) return;
  window.__catBackNavigationInitialized = true;

  const desktopViewport = window.matchMedia('(min-width: 768px)');
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  const focusableSelector = [
    'a[href]',
    'button:not([disabled])',
    'input:not([disabled])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    '[tabindex]:not([tabindex="-1"])'
  ].join(',');
  let activeOverlay;
  let activeTrigger;
  let closeTimer;
  let focusTimer;
  let openFrame;

  const updateLinkTargets = () => {
    const links = document.querySelectorAll('[data-open-in-new-tab-on-desktop]');
    links.forEach((link) => {
      if (desktopViewport.matches) {
        link.target = '_blank';
      } else {
        link.removeAttribute('target');
      }
    });
  };

  const drawerIsOpen = () => Boolean(
    activeOverlay?.isConnected && !activeOverlay.hidden && activeOverlay.getAttribute('aria-hidden') === 'false'
  );

  const openAccountNavigation = (trigger) => {
    const overlay = document.querySelector('[data-account-navigation-overlay]');
    const drawer = overlay?.querySelector('.account-navigation-drawer');
    if (!overlay || !drawer) return;

    window.clearTimeout(closeTimer);
    window.clearTimeout(focusTimer);
    window.cancelAnimationFrame(openFrame);
    activeOverlay = overlay;
    activeTrigger = trigger;
    overlay.hidden = false;
    overlay.setAttribute('aria-hidden', 'false');
    trigger.setAttribute('aria-expanded', 'true');
    document.body.classList.add('account-navigation-open');

    openFrame = window.requestAnimationFrame(() => {
      overlay.classList.add('is-open');
      focusTimer = window.setTimeout(() => {
        const closeButton = drawer.querySelector('[data-account-navigation-close]');
        (closeButton || drawer).focus();
      }, reducedMotion.matches ? 0 : 80);
    });
  };

  const closeAccountNavigation = ({ restoreFocus = true, immediate = false } = {}) => {
    const overlay = activeOverlay || document.querySelector('[data-account-navigation-overlay]');
    if (!overlay) return;

    const trigger = activeTrigger || document.querySelector('[data-account-navigation-open]');
    window.cancelAnimationFrame(openFrame);
    window.clearTimeout(focusTimer);
    overlay.classList.remove('is-open');
    if (restoreFocus && trigger?.isConnected) trigger.focus();
    overlay.setAttribute('aria-hidden', 'true');
    trigger?.setAttribute('aria-expanded', 'false');
    document.body.classList.remove('account-navigation-open');

    const finish = () => {
      overlay.hidden = true;
      if (activeOverlay === overlay) activeOverlay = undefined;
      activeTrigger = undefined;
    };

    window.clearTimeout(closeTimer);
    if (immediate || reducedMotion.matches) finish();
    else closeTimer = window.setTimeout(finish, 230);
  };

  const resetAccountNavigation = () => {
    window.clearTimeout(closeTimer);
    window.clearTimeout(focusTimer);
    window.cancelAnimationFrame(openFrame);
    document.body?.classList.remove('account-navigation-open');
    document.querySelectorAll('[data-account-navigation-overlay]').forEach((overlay) => {
      overlay.classList.remove('is-open');
      overlay.hidden = true;
      overlay.setAttribute('aria-hidden', 'true');
    });
    document.querySelectorAll('[data-account-navigation-open]').forEach((trigger) => {
      trigger.setAttribute('aria-expanded', 'false');
    });
    activeOverlay = undefined;
    activeTrigger = undefined;
  };

  document.addEventListener('click', (event) => {
    const target = event.target instanceof Element ? event.target : event.target?.parentElement;
    if (!target) return;

    const trigger = target.closest('[data-account-navigation-open]');
    if (trigger) {
      event.preventDefault();
      if (drawerIsOpen()) closeAccountNavigation();
      else openAccountNavigation(trigger);
      return;
    }

    const overlay = target.closest('[data-account-navigation-overlay]');
    if (!overlay) return;

    if (target === overlay || target.closest('[data-account-navigation-close]')) {
      event.preventDefault();
      closeAccountNavigation();
      return;
    }

    if (target.closest('a[href]')) {
      closeAccountNavigation({ restoreFocus: false });
    }
  });

  document.addEventListener('keydown', (event) => {
    if (!drawerIsOpen()) return;

    if (event.key === 'Escape') {
      event.preventDefault();
      closeAccountNavigation();
      return;
    }

    if (event.key !== 'Tab') return;
    const drawer = activeOverlay.querySelector('.account-navigation-drawer');
    if (!drawer) return;
    const focusable = [...drawer.querySelectorAll(focusableSelector)]
      .filter((element) => !element.hidden && element.getClientRects().length > 0);
    if (!focusable.length) {
      event.preventDefault();
      drawer.focus();
      return;
    }

    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  });

  updateLinkTargets();
  desktopViewport.addEventListener('change', updateLinkTargets);
  document.addEventListener('turbo:before-cache', () => closeAccountNavigation({ restoreFocus: false, immediate: true }));
  document.addEventListener('turbo:before-render', () => closeAccountNavigation({ restoreFocus: false, immediate: true }));
  document.addEventListener('turbo:load', () => {
    resetAccountNavigation();
    updateLinkTargets();
  });
})();

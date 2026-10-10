(() => {
  const done = document.querySelector('[data-authorization-done]');
  const countdown = document.querySelector('[data-authorization-countdown]');
  if (!done || !countdown) return;

  const deadline = performance.now() + 5000;
  let redirectTimer;
  const interval = window.setInterval(() => {
    const remaining = Math.max(0, Math.ceil((deadline - performance.now()) / 1000));
    countdown.textContent = String(remaining);
    if (remaining === 0) {
      window.clearInterval(interval);
      // Let Done(0) paint before returning to the connected workspace.
      redirectTimer = window.setTimeout(() => window.location.replace('/tiktok-affiliate'), 150);
    }
  }, 1000);

  const stopCountdown = () => {
    window.clearInterval(interval);
    window.clearTimeout(redirectTimer);
  };
  done.addEventListener('click', event => {
    if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
    event.preventDefault();
    stopCountdown();
    window.location.replace('/tiktok-affiliate');
  });
  window.addEventListener('pagehide', stopCountdown, { once: true });
})();

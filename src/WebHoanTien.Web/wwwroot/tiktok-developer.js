(() => {
  const initializedRoots = new WeakSet();
  const init = () => {
    const root = document.querySelector('[data-tiktok-developer]');
    if (!root || initializedRoots.has(root)) return;
    initializedRoots.add(root);
    root.querySelectorAll('[data-developer-submit]').forEach(form => form.addEventListener('submit', event => {
      if (event.defaultPrevented) return;
      if (form.dataset.submitting) { event.preventDefault(); return; }
      form.dataset.submitting = 'true';
      const button = form.querySelector('button[type="submit"]');
      if (button) { button.disabled = true; button.setAttribute('aria-busy', 'true'); }
    }));
    root.querySelector('[data-copy-developer-key]')?.addEventListener('click', async () => {
      const input = root.querySelector('#new-developer-key');
      const status = root.querySelector('[data-developer-copy-status]');
      if (!input?.value) { status.textContent = 'Key chỉ hiển thị tại thời điểm tạo.'; return; }
      try { await navigator.clipboard.writeText(input.value); status.textContent = 'Đã sao chép.'; }
      catch { input.focus(); input.select(); status.textContent = 'Hãy sao chép key đang được chọn.'; }
    });
  };
  if (window.catBackTikTokDeveloper) { window.catBackTikTokDeveloper(); return; }
  window.catBackTikTokDeveloper = init;
  document.addEventListener('turbo:load', init); document.addEventListener('DOMContentLoaded', init);
  const clearOneTimeKey = () => {
    const input = document.querySelector('#new-developer-key');
    if (input) { input.value = ''; input.defaultValue = ''; input.removeAttribute('value'); }
  };
  document.addEventListener('turbo:before-cache', clearOneTimeKey);
  window.addEventListener('pagehide', clearOneTimeKey);
  window.addEventListener('pageshow', () => document.querySelectorAll('[data-developer-submit][data-submitting]').forEach(form => {
    delete form.dataset.submitting;
    const button = form.querySelector('button[type="submit"]');
    window.CatBackLoading?.setButtonLoading(button, false);
    if (button) { button.disabled = false; button.removeAttribute('aria-busy'); }
  }));
  init();
})();

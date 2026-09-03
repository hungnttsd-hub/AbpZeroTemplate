window.CatBackSpa.mount('password-visibility', ({ signal }) => {
  document.querySelectorAll('[data-password-toggle]').forEach((button) => {
    const shell = button.closest('.password-input-shell');
    const input = shell?.querySelector('input');
    if (!(button instanceof HTMLButtonElement) || !(input instanceof HTMLInputElement)) {
      return;
    }

    const updateState = (isVisible) => {
      const label = isVisible ? 'Ẩn mật khẩu' : 'Hiện mật khẩu';
      input.type = isVisible ? 'text' : 'password';
      button.setAttribute('aria-pressed', String(isVisible));
      button.setAttribute('aria-label', label);
      button.title = label;
    };

    updateState(input.type === 'text');
    button.addEventListener('click', () => updateState(input.type === 'password'), { signal });
  });
});

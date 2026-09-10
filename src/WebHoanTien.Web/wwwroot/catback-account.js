(() => {
  if (window.CatBackAccounts) return;
  window.CatBackAccounts = true;
  const initialize = () => window.CatBackSpa.mount('catback-accounts', ({ signal }) => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const show = (target, message, success = false) => {
      if (!target) return;
      target.hidden = false; target.textContent = message; target.dataset.state = success ? 'success' : 'error';
    };
    const api = async (url, method = 'GET', input) => {
      const response = await fetch(url, { method, credentials: 'same-origin', cache: 'no-store', signal,
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() || '', 'Accept': 'application/json' },
        body: input === undefined ? undefined : JSON.stringify(input) });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) {
        const error = new Error(result.message || result.error?.message || (response.status === 401 ? 'Phiên đã hết hạn. Vui lòng đăng nhập lại.' : 'Không thể hoàn tất. Vui lòng thử lại.'));
        error.pending = result.pending === true; throw error;
      }
      return result;
    };
    const navigate = url => { window.Turbo?.cache?.clear(); window.location.assign(url); };
    let toastTimer;
    const toast = document.createElement('div');
    toast.className = 'cb-account-toast'; toast.setAttribute('role', 'status'); toast.hidden = true;
    document.body.append(toast);
    const notify = message => { clearTimeout(toastTimer); toast.textContent = message; toast.hidden = false; toastTimer = setTimeout(() => toast.hidden = true, 3000); };
    document.querySelectorAll('[data-account-api]').forEach(form => form.addEventListener('submit', async event => {
      event.preventDefault();
      if (form.dataset.busy) return;
      const input = Object.fromEntries(new FormData(form));
      delete input.__RequestVerificationToken;
      if ('acceptedTerms' in input) input.acceptedTerms = input.acceptedTerms === 'on' || input.acceptedTerms === 'true';
      const status = form.querySelector('[data-account-status]');
      if ('confirmPassword' in input && input.password !== input.confirmPassword) { show(status, 'Mật khẩu xác nhận không khớp.'); return; }
      form.dataset.busy = 'true';
      const button = form.querySelector('button:not([type=button])');
      const originalButtonText = button?.textContent;
      const isAnonymousCreate = form.dataset.accountApi === '/api/account/anonymous';
      if (button) button.disabled = true;
      if (button && isAnonymousCreate) button.textContent = 'Đang tạo tài khoản…';
      form.setAttribute('aria-busy', 'true');
      try {
        const result = await api(form.dataset.accountApi, form.dataset.accountMethod || 'POST', input);
        if (result.redirectUrl) { navigate(result.redirectUrl); return; }
        show(status, result.message || 'Đã lưu thành công.', true);
        if (result.pending) { document.querySelector('[data-upgrade-pending]')?.removeAttribute('hidden'); form.querySelectorAll('input[type=password]').forEach(i => i.value = ''); }
      } catch (error) {
        if (error.name !== 'AbortError') show(status, error.message);
        if (error.pending) document.querySelector('[data-upgrade-pending]')?.removeAttribute('hidden');
      } finally {
        delete form.dataset.busy;
        form.removeAttribute('aria-busy');
        if (button) {
          button.disabled = false;
          if (isAnonymousCreate) button.textContent = originalButtonText;
        }
      }
    }, { signal }));
    const confirm = async (title, message) => window.CatsBackModal?.confirm
      ? window.CatsBackModal.confirm({ title, message, confirmText: 'Tiếp tục', cancelText: 'Quay lại' }) : window.confirm(message);
    const createDialog = document.querySelector('[data-anonymous-create]');
    let createTrigger;
    if (createDialog) {
      const isCreating = () => !!createDialog.querySelector('form[data-busy]');
      const openCreate = source => {
        if (createDialog.open) return;
        createTrigger = source;
        createDialog.showModal();
        createDialog.querySelector('h2').focus();
      };
      document.querySelectorAll('[data-anonymous-open]').forEach(button =>
        button.addEventListener('click', () => openCreate(button), { signal }));
      createDialog.querySelectorAll('[data-anonymous-dismiss]').forEach(button =>
        button.addEventListener('click', () => { if (!isCreating()) createDialog.close(); }, { signal }));
      createDialog.addEventListener('cancel', event => { if (isCreating()) event.preventDefault(); }, { signal });
      createDialog.addEventListener('close', () => createTrigger?.focus(), { signal });
      if (createDialog.dataset.autoOpen === 'true') {
        openCreate(document.querySelector('[data-anonymous-open]'));
        const url = new URL(location.href);
        url.searchParams.delete('showAnonymous');
        history.replaceState(history.state, '', url);
      }
    }
    const recovery = document.querySelector('[data-recovery-page]');
    let recoveryCode = '', acknowledged = false;
    if (recovery) {
      const codeElement = recovery.querySelector('[data-recovery-code]');
      const status = recovery.querySelector('[data-recovery-status]');
      const setCode = code => {
        recoveryCode = code; codeElement.replaceChildren();
        const groups = code.split('-');
        for (let i = 0; i < groups.length; i++) { const span = document.createElement('span'); span.textContent = groups[i] + (i < groups.length - 1 ? '-' : ''); codeElement.append(span); }
      };
      const loading = busy => recovery.querySelectorAll('[data-recovery-copy],[data-recovery-save],[data-recovery-save-home],[data-recovery-ack],[data-recovery-regenerate]').forEach(b => b.disabled = busy || !recoveryCode);
      loading(true);
      api('/api/account/anonymous/recovery').then(result => setCode(result.recoveryCode)).catch(error => {
        if (error.name !== 'AbortError') { codeElement.textContent = 'Chưa tải được mã'; show(status, error.message + ' Hãy tải lại trang.'); }
      }).finally(() => loading(false));
      recovery.querySelector('[data-recovery-copy]').addEventListener('click', async () => {
        if (!recoveryCode) return;
        try { await navigator.clipboard.writeText(recoveryCode); show(status, 'Đã sao chép mã', true); notify('Đã sao chép mã'); }
        catch { show(status, 'Không thể sao chép tự động. Hãy chọn mã để sao chép hoặc dùng Lưu ảnh.'); }
      }, { signal });
      const saveRecoveryImage = async () => {
        if (!recoveryCode || signal.aborted) return false;
        try {
          await document.fonts.ready;
          if (signal.aborted) return false;
          const canvas = document.createElement('canvas'); canvas.width = 1000; canvas.height = 620;
          const ctx = canvas.getContext('2d'); ctx.fillStyle = '#fff7df'; ctx.fillRect(0, 0, 1000, 620);
          ctx.fillStyle = '#031e45'; ctx.font = 'bold 42px sans-serif'; ctx.fillText('CATBACK • MÃ KHÔI PHỤC', 60, 90);
          ctx.font = '28px sans-serif'; ctx.fillText(recovery.dataset.recoveryUsername || '', 60, 155);
          ctx.font = 'bold 35px monospace'; const groups = recoveryCode.split('-');
          ctx.fillText(groups.slice(0, 5).join('-'), 60, 265); ctx.fillText(groups.slice(5).join('-'), 60, 325);
          ctx.font = '24px sans-serif'; ctx.fillText('Giữ kín mã này để khôi phục tài khoản của bạn.', 60, 450);
          ctx.fillText('Mã cũ hết hiệu lực khi đổi mã hoặc nâng cấp tài khoản.', 60, 500);
          const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
          if (signal.aborted) return false;
          if (!blob) { show(status, 'Không thể lưu ảnh. Hãy sao chép mã.'); return false; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url; link.download = 'CatBack-ma-khoi-phuc.png';
          document.body.append(link);
          link.click(); link.remove();
          setTimeout(() => URL.revokeObjectURL(url), 1500);
          show(status, 'Đã tạo ảnh mã khôi phục. Hãy kiểm tra tệp đã tải.', true);
          return true;
        } catch { show(status, 'Không thể lưu ảnh trên trình duyệt này. Hãy sao chép mã.'); return false; }
      };
      let savingImage = false;
      const saveRecovery = async goHome => {
        if (savingImage || !recoveryCode) return;
        savingImage = true;
        loading(true);
        try {
          if (await saveRecoveryImage() && goHome) {
            // Let the browser dispatch the download before unloading this page.
            await new Promise(resolve => setTimeout(resolve, 250));
            if (!signal.aborted) navigate('/');
          }
        } finally { savingImage = false; loading(false); }
      };
      recovery.querySelector('[data-recovery-save]').addEventListener('click', () => saveRecovery(false), { signal });
      recovery.querySelector('[data-recovery-save-home]')?.addEventListener('click', () => saveRecovery(true), { signal });
      recovery.querySelector('[data-recovery-ack]').addEventListener('click', event => {
        if (!recoveryCode) return; acknowledged = true; event.currentTarget.textContent = 'Đã xác nhận lưu mã'; show(status, 'Bạn có thể tiếp tục vào app.', true);
      }, { signal });
      recovery.querySelectorAll('[data-recovery-continue]').forEach(a => a.addEventListener('click', async event => {
        event.preventDefault(); if (acknowledged || await confirm('Bạn chưa xác nhận lưu mã', 'Nếu mất dữ liệu trình duyệt và chưa lưu mã, bạn có thể mất quyền truy cập tài khoản. Bạn vẫn muốn tiếp tục?')) navigate(a.href);
      }, { signal }));
      recovery.querySelector('[data-recovery-regenerate]').addEventListener('click', async () => {
        if (!await confirm('Đổi mã khôi phục', 'Mã cũ sẽ hết hiệu lực ngay. Hãy lưu lại mã mới sau khi tạo.')) return;
        loading(true);
        try { const result = await api('/api/account/anonymous/recovery/regenerate', 'POST', {}); setCode(result.recoveryCode); acknowledged = false; recovery.querySelector('[data-recovery-ack]').textContent = 'Tôi đã lưu mã'; show(status, 'Đã tạo mã mới. Hãy lưu lại mã này.', true); }
        catch (error) { show(status, error.message); }
        finally { loading(false); }
      }, { signal });
    }
    document.querySelector('[data-upgrade-resend]')?.addEventListener('click', async event => {
      const button = event.currentTarget; button.disabled = true;
      try { show(document.querySelector('[data-resend-status]'), (await api('/api/account/upgrade/resend', 'POST', {})).message, true); }
      catch (error) { show(document.querySelector('[data-resend-status]'), error.message); }
      finally { button.disabled = false; }
    }, { signal });
    document.querySelector('[data-device-details]')?.addEventListener('toggle', async event => {
      if (!event.currentTarget.open) return;
      try { const result = await api('/api/account/device/current'); show(document.querySelector('[data-device-status]'), result.remembered ? 'Thiết bị được ghi nhớ tới ' + new Date(result.expiresAt).toLocaleDateString('vi-VN') + '. Đăng xuất thường vẫn giữ ghi nhớ này.' : 'Thiết bị này không được ghi nhớ.', true); }
      catch (error) { show(document.querySelector('[data-device-status]'), error.message); }
    }, { signal });
    document.querySelector('[data-device-forget]')?.addEventListener('click', async event => {
      const button = event.currentTarget;
      if (!await confirm('Quên thiết bị này', 'Hãy lưu mã khôi phục trước. Sau khi quên thiết bị, bạn cần mã để đăng nhập lại.')) return;
      button.disabled = true;
      try { navigate((await api('/api/account/device/current', 'DELETE')).redirectUrl); }
      catch (error) { show(document.querySelector('[data-forget-status]'), error.message); button.disabled = false; }
    }, { signal });
    const dialog = document.querySelector('[data-anonymous-withdraw]');
    let trigger;
    const openWithdraw = source => { if (!dialog || dialog.open) return; trigger = source; dialog.showModal(); };
    if (dialog) {
      document.addEventListener('click', event => {
        const link = event.target.closest('a[href]');
        if (link && new URL(link.href, location.href).pathname.toLowerCase() === '/wallet/withdraw') { event.preventDefault(); event.stopImmediatePropagation(); openWithdraw(link); }
      }, { signal, capture: true });
      dialog.querySelectorAll('[data-withdraw-dismiss]').forEach(button => button.addEventListener('click', () => dialog.close(), { signal }));
      dialog.addEventListener('close', () => trigger?.focus(), { signal });
      document.addEventListener('catback:registration-required', () => openWithdraw(document.activeElement), { signal });
      if (new URLSearchParams(location.search).get('registrationRequired') === 'true') openWithdraw(document.querySelector('.wallet-action-withdraw'));
    }
    return () => { clearTimeout(toastTimer); toast.remove(); recoveryCode = ''; if (recovery) recovery.querySelector('[data-recovery-code]').replaceChildren(); if (dialog?.open) dialog.close(); if (createDialog?.open) createDialog.close(); };
  });
  initialize(); document.addEventListener('turbo:load', initialize);
})();

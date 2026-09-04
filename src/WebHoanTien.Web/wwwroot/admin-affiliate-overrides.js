window.CatBackSpa.mount('admin-affiliate-overrides', ({ signal }) => {
  const form = document.querySelector('[data-affiliate-override-form]');
  const removeForm = document.getElementById('affiliate-override-remove-form');
  const list = document.querySelector('[data-affiliate-override-list]');
  const empty = document.querySelector('[data-affiliate-override-empty]');
  const status = document.querySelector('[data-affiliate-override-status]');
  if (!form || !removeForm || !list) return;

  const fields = {
    email: form.querySelector('[name="userEmail"]'),
    affiliateId: form.querySelector('[name="affiliateId"]'),
    note: form.querySelector('[name="adminNote"]')
  };

  const showStatus = (message, state = 'success') => {
    if (!status) return;
    status.textContent = message;
    status.dataset.state = state;
  };

  const formatUpdatedAt = (value) => {
    if (!value) return 'vừa xong';
    return new Intl.DateTimeFormat('vi-VN', {
      day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
    }).format(new Date(value));
  };

  const createButton = (text, className, attribute) => {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = className;
    button.textContent = text;
    button.setAttribute(attribute, '');
    return button;
  };

  const renderRow = (item) => {
    let row = list.querySelector(`[data-user-id="${CSS.escape(String(item.userId))}"]`);
    if (!row) {
      row = document.createElement('article');
      row.className = 'affiliate-override-row';

      const user = document.createElement('div');
      user.className = 'affiliate-override-user';
      const email = document.createElement('strong');
      email.dataset.userEmail = '';
      const updated = document.createElement('span');
      user.append(email, updated);

      const affiliateId = document.createElement('code');
      affiliateId.dataset.affiliateId = '';
      const note = document.createElement('p');
      note.dataset.adminNote = '';
      const actions = document.createElement('div');
      actions.className = 'affiliate-override-actions';
      actions.append(
        createButton('Sửa', 'button button-ghost', 'data-affiliate-override-edit'),
        createButton('Về ID mặc định', 'button button-danger', 'data-affiliate-override-remove')
      );
      row.append(user, affiliateId, note, actions);
      list.prepend(row);
    }

    row.dataset.userId = item.userId;
    row.querySelector('[data-user-email]').textContent = item.userEmail;
    row.querySelector('.affiliate-override-user span').textContent =
      `Shopee · cập nhật ${formatUpdatedAt(item.lastModificationTime || item.creationTime)}`;
    row.querySelector('[data-affiliate-id]').textContent = item.affiliateId;
    row.querySelector('[data-admin-note]').textContent = item.adminNote || 'Không có ghi chú';
    empty?.setAttribute('hidden', '');
  };

  const postForm = async (targetForm, data) => {
    const token = form.querySelector('[name="__RequestVerificationToken"]')?.value;
    if (token) data.set('__RequestVerificationToken', token);
    const response = await fetch(targetForm.action, {
      method: 'POST', body: data, credentials: 'same-origin',
      headers: { 'X-Requested-With': 'XMLHttpRequest', Accept: 'application/json' }
    });
    const result = await response.json().catch(() => ({}));
    if (!response.ok) throw new Error(result.error || 'Không thể cập nhật cấu hình lúc này.');
    return result;
  };

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    if (!form.checkValidity()) return form.reportValidity();
    const submit = form.querySelector('[type="submit"]');
    window.CatBackLoading?.setButtonLoading(submit, true, { text: 'Đang lưu...' });
    if (!window.CatBackLoading) submit.disabled = true;
    showStatus('Đang lưu cấu hình...');
    try {
      const result = await postForm(form, new FormData(form));
      renderRow(result.item);
      form.reset();
      showStatus(`Đã áp dụng Affiliate ID riêng cho ${result.item.userEmail}.`);
    } catch (error) {
      showStatus(error.message, 'error');
    } finally {
      window.CatBackLoading?.setButtonLoading(submit, false);
      if (!window.CatBackLoading) submit.disabled = false;
    }
  }, { signal });

  list.addEventListener('click', async (event) => {
    const target = event.target instanceof Element ? event.target : null;
    const row = target?.closest('.affiliate-override-row');
    if (!row) return;

    if (target.closest('[data-affiliate-override-edit]')) {
      fields.email.value = row.querySelector('[data-user-email]').textContent.trim();
      fields.affiliateId.value = row.querySelector('[data-affiliate-id]').textContent.trim();
      const note = row.querySelector('[data-admin-note]').textContent.trim();
      fields.note.value = note === 'Không có ghi chú' ? '' : note;
      form.scrollIntoView({ behavior: 'smooth', block: 'center' });
      fields.affiliateId.focus();
      return;
    }

    const removeButton = target.closest('[data-affiliate-override-remove]');
    if (!removeButton) return;
    const email = row.querySelector('[data-user-email]').textContent.trim();
    if (!window.confirm(`Đưa ${email} về Affiliate ID mặc định?`)) return;

    window.CatBackLoading?.setButtonLoading(removeButton, true, { text: 'Đang xử lý...' });
    if (!window.CatBackLoading) removeButton.disabled = true;
    try {
      const data = new FormData();
      data.set('userId', row.dataset.userId);
      await postForm(removeForm, data);
      row.remove();
      if (!list.querySelector('.affiliate-override-row')) empty?.removeAttribute('hidden');
      showStatus(`Đã đưa ${email} về Affiliate ID mặc định.`);
    } catch (error) {
      showStatus(error.message, 'error');
    } finally {
      window.CatBackLoading?.setButtonLoading(removeButton, false);
      if (!window.CatBackLoading) removeButton.disabled = false;
    }
  }, { signal });
});

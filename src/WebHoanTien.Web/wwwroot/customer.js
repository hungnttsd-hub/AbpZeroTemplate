(() => {
  const linkForm = document.getElementById('link-form');
  const createButton = document.getElementById('create-button');
  const linkInput = document.getElementById('affiliate-url');
  const clearButton = document.getElementById('affiliate-url-clear');
  const urlStatus = document.getElementById('url-status');
  const dashboardLinks = document.querySelector('.dashboard-links');
  const createButtonContent = createButton?.innerHTML;

  const closeActionSheet = (sheet) => {
    if (!sheet) return;
    sheet.hidden = true;
    sheet.setAttribute('aria-hidden', 'true');
    document.body.classList.remove('has-link-action-sheet');
  };

  const syncClearButton = () => {
    if (clearButton) clearButton.hidden = !linkInput?.value;
  };

  const showUrlStatus = (message, state) => {
    if (!urlStatus) return;
    urlStatus.textContent = message;
    urlStatus.dataset.state = state;
  };

  const showVisibilityError = (message) => {
    if (window.CatsBackModal?.info) {
      void window.CatsBackModal.info({
        title: 'Không thể ẩn link',
        message,
        confirmText: 'Đã hiểu'
      });
      return;
    }

    window.alert(message);
  };

  const findLinkCard = (linkId) => Array.from(document.querySelectorAll('.affiliate-link-card'))
    .find((card) => card.dataset.linkId === String(linkId));

  const findVisibilityForms = (linkId) => Array.from(document.querySelectorAll('[data-link-visibility-form]'))
    .filter((form) => form.dataset.linkId === String(linkId));

  const getActionSheetId = (linkId) => `link-actions-${String(linkId).replace(/-/g, '')}`;

  const renderEmptyLinkState = () => {
    const list = document.querySelector('.affiliate-link-list');
    if (!list || list.querySelector('.affiliate-link-card')) return;
    const template = document.getElementById('affiliate-link-empty-template');
    if (!template) return;
    list.replaceWith(template.content.cloneNode(true));
  };

  const updateCardVisibility = (card, linkId, isHidden) => {
    card?.classList.toggle('is-hidden', isHidden);
    const details = card?.querySelector('.affiliate-link-details');
    let badge = details?.querySelector('.affiliate-hidden-badge');
    if (isHidden && details && !badge) {
      badge = document.createElement('span');
      badge.className = 'affiliate-hidden-badge';
      badge.textContent = 'Đã ẩn';
      details.querySelector('.affiliate-store')?.insertAdjacentElement('afterend', badge);
    } else if (!isHidden) {
      badge?.remove();
    }

    findVisibilityForms(linkId).forEach((visibilityForm) => {
      const actionInput = visibilityForm.querySelector('input[name="visibilityAction"]');
      const actionLabel = visibilityForm.querySelector('[data-visibility-label]');
      const actionButton = visibilityForm.querySelector('button[type="submit"]');
      const actionIcon = visibilityForm.querySelector('[data-visibility-icon]');
      if (actionInput) actionInput.value = isHidden ? 'show' : 'hide';
      if (actionLabel) {
        actionLabel.textContent = isHidden ? 'Bỏ ẩn link' : 'Ẩn link khỏi danh sách';
      }
      const actionDescription = isHidden ? 'Bỏ ẩn link' : 'Ẩn link khỏi danh sách';
      actionButton?.setAttribute('aria-label', actionDescription);
      actionButton?.setAttribute('title', actionDescription);
      if (actionIcon) actionIcon.src = isHidden ? '/catback/icons/eye-off.svg' : '/catback/icons/trash.svg';
    });
  };

  const setProductImage = (card, imageUrl, productName) => {
    const imageBox = card.querySelector('.affiliate-product-image');
    if (!imageBox) return;
    let image = Array.from(imageBox.children).find((element) => element.tagName === 'IMG');
    let placeholder = Array.from(imageBox.children).find((element) => element.tagName === 'SPAN');

    if (imageUrl) {
      if (!image) {
        image = document.createElement('img');
        imageBox.prepend(image);
      }
      image.src = imageUrl;
      image.alt = `Ảnh ${productName}`;
      image.loading = 'lazy';
      image.hidden = false;
      if (placeholder) placeholder.hidden = true;
      return;
    }

    if (image) image.hidden = true;
    if (!placeholder) {
      placeholder = document.createElement('span');
      const placeholderImage = document.createElement('img');
      placeholderImage.src = '/catback/icons/shopping-bag.svg';
      placeholderImage.alt = '';
      placeholder.append(placeholderImage);
      imageBox.append(placeholder);
    }
    placeholder.hidden = false;
  };

  const setLinkCardData = (card, sheet, link) => {
    const linkId = String(link.id);
    const actionSheetId = getActionSheetId(linkId);
    const productName = link.productName || 'Sản phẩm Shopee';
    card.dataset.linkId = linkId;
    card.classList.remove('is-hidden', 'is-removing');
    card.classList.add('is-new');

    setProductImage(card, link.imageUrl, productName);
    const productNameElement = card.querySelector('.affiliate-link-details h3');
    if (productNameElement) productNameElement.textContent = productName;

    const estimate = card.querySelector('.affiliate-estimate');
    if (estimate) {
      estimate.textContent = link.estimatedCommissionLabel
        ? `Hoàn lại dự kiến ${link.estimatedCommissionLabel}`
        : 'Chưa có ước tính hoa hồng';
      estimate.classList.toggle('unavailable', !link.estimatedCommissionLabel);
    }

    const buyButton = card.querySelector('.affiliate-buy-button');
    if (buyButton) {
      buyButton.href = link.redirectUrl;
      if (window.matchMedia('(min-width: 768px)').matches) buyButton.target = '_blank';
      else buyButton.removeAttribute('target');
    }

    if (sheet) {
      sheet.id = actionSheetId;
      sheet.hidden = true;
      sheet.setAttribute('aria-hidden', 'true');
    }

    [card, sheet].filter(Boolean).forEach((root) => {
      root.querySelectorAll('[data-copy-url]').forEach((button) => {
        button.dataset.copyUrl = link.redirectUrl || '';
        button.hidden = !link.redirectUrl;
      });
      root.querySelectorAll('[data-link-visibility-form]').forEach((form) => {
        form.dataset.linkId = linkId;
        form.dataset.actionSheetId = actionSheetId;
        const linkIdInput = form.querySelector('input[name="linkId"]');
        const visibilityInput = form.querySelector('input[name="visibilityAction"]');
        if (linkIdInput) linkIdInput.value = linkId;
        if (visibilityInput) visibilityInput.value = 'hide';
      });
    });

    updateCardVisibility(card, linkId, false);
    window.setTimeout(() => card.classList.remove('is-new'), 1800);
  };

  const removeOverflowCards = (list) => {
    Array.from(list.querySelectorAll('.affiliate-link-card')).slice(5).forEach((card) => {
      const form = card.querySelector('[data-link-visibility-form]');
      document.getElementById(form?.dataset.actionSheetId)?.remove();
      card.remove();
    });
  };

  const upsertLinkCard = (link) => {
    if (!dashboardLinks || !link?.id) return;
    const linkId = String(link.id);
    let card = findLinkCard(linkId);
    let sheet;

    if (card) {
      const form = card.querySelector('[data-link-visibility-form]');
      sheet = document.getElementById(form?.dataset.actionSheetId || getActionSheetId(linkId));
    } else {
      const template = document.getElementById('affiliate-link-card-template');
      if (!template) return;
      const fragment = template.content.cloneNode(true);
      card = fragment.querySelector('.affiliate-link-card');
      sheet = fragment.querySelector('.affiliate-action-sheet');
    }

    dashboardLinks.querySelector('.dashboard-link-empty')?.remove();
    let list = dashboardLinks.querySelector('.affiliate-link-list');
    if (!list) {
      list = document.createElement('div');
      list.className = 'affiliate-link-list';
      dashboardLinks.insertBefore(list, dashboardLinks.querySelector('template'));
    }

    setLinkCardData(card, sheet, link);
    list.prepend(card);
    if (sheet && !sheet.isConnected) dashboardLinks.append(sheet);
    removeOverflowCards(list);
  };

  const submitLinkVisibility = async (form) => {
    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton?.disabled) return;

    const formData = new FormData(form);
    const linkId = formData.get('linkId');
    const requestedHidden = formData.get('visibilityAction') === 'hide';
    const sheet = form.closest('.affiliate-action-sheet') || document.getElementById(form.dataset.actionSheetId);
    const card = findLinkCard(linkId);

    if (submitButton) submitButton.disabled = true;
    try {
      const response = await fetch(form.action, {
        method: 'POST',
        body: formData,
        credentials: 'same-origin',
        headers: { 'X-Requested-With': 'XMLHttpRequest', Accept: 'application/json' }
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(result.error || 'Visibility update failed');

      closeActionSheet(sheet);
      if (result.isHidden) {
        card?.classList.add('is-removing');
        let removalFallback;
        const finishRemoval = () => {
          window.clearTimeout(removalFallback);
          card?.remove();
          sheet?.remove();
          renderEmptyLinkState();
        };
        card?.addEventListener('animationend', finishRemoval, { once: true });
        removalFallback = window.setTimeout(finishRemoval, 500);
      } else {
        updateCardVisibility(card, linkId, result.isHidden ?? requestedHidden);
      }
    } catch (error) {
      showVisibilityError(error.message || 'Không thể cập nhật link lúc này. Vui lòng thử lại.');
    } finally {
      if (submitButton) submitButton.disabled = false;
    }
  };

  linkInput?.addEventListener('input', syncClearButton);
  clearButton?.addEventListener('click', () => {
    linkInput.value = '';
    syncClearButton();
    linkInput.focus();
  });
  syncClearButton();

  linkForm?.addEventListener('submit', async (event) => {
    event.preventDefault();
    if (!createButton || !linkForm.checkValidity() || createButton.disabled) return;

    createButton.disabled = true;
    createButton.textContent = 'Đang tạo link...';
    try {
      const response = await fetch(linkForm.action, {
        method: 'POST',
        body: new FormData(linkForm),
        credentials: 'same-origin',
        headers: { 'X-Requested-With': 'XMLHttpRequest', Accept: 'application/json' }
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(result.error || 'Không thể tạo link mua hàng lúc này.');
      if (result.requiresLogin) {
        window.location.assign(result.redirectUrl);
        return;
      }

      upsertLinkCard(result.link);
      linkInput.value = '';
      syncClearButton();
      showUrlStatus(result.message, 'success');
    } catch (error) {
      showUrlStatus(error.message || 'Không thể tạo link mua hàng lúc này. Vui lòng thử lại sau.', 'error');
    } finally {
      createButton.disabled = false;
      if (createButtonContent !== undefined) createButton.innerHTML = createButtonContent;
    }
  });

  document.addEventListener('submit', (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.matches('[data-link-visibility-form]')) return;
    event.preventDefault();
    void submitLinkVisibility(form);
  });

  document.addEventListener('click', async (event) => {
    const target = event.target instanceof Element ? event.target : null;
    if (!target) return;

    const focusButton = target.closest('[data-focus-link-input]');
    if (focusButton) {
      linkForm?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      window.setTimeout(() => linkInput?.focus(), 350);
      return;
    }

    const openButton = target.closest('[data-link-actions-open]');
    if (openButton) {
      const sheet = document.getElementById(openButton.dataset.linkActionsOpen);
      if (!sheet) return;
      sheet.hidden = false;
      sheet.setAttribute('aria-hidden', 'false');
      document.body.classList.add('has-link-action-sheet');
      sheet.querySelector('.affiliate-action-row, .affiliate-action-cancel')?.focus();
      return;
    }

    const closeButton = target.closest('[data-link-actions-close]');
    if (closeButton) {
      closeActionSheet(closeButton.closest('.affiliate-action-sheet'));
      return;
    }

    const copyButton = target.closest('[data-copy-url]');
    if (!copyButton || !copyButton.dataset.copyUrl) return;
    const label = copyButton.querySelector('[data-copy-label]') || copyButton.querySelector('span') || copyButton;
    const originalText = label.textContent;
    try {
      const stableUrl = new URL(copyButton.dataset.copyUrl, window.location.origin).href;
      await navigator.clipboard.writeText(stableUrl);
      label.textContent = 'Đã sao chép link';
      copyButton.classList.add('is-copied');
      copyButton.setAttribute('aria-label', 'Đã sao chép link');
      if (copyButton.hasAttribute('data-link-actions-close-after-copy')) {
        window.setTimeout(() => closeActionSheet(copyButton.closest('.affiliate-action-sheet')), 450);
      }
      window.setTimeout(() => {
        label.textContent = originalText;
        copyButton.classList.remove('is-copied');
        copyButton.setAttribute('aria-label', originalText);
      }, 1800);
    } catch {
      label.textContent = 'Không thể sao chép';
      copyButton.setAttribute('aria-label', 'Không thể sao chép');
    }
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') closeActionSheet(document.querySelector('.affiliate-action-sheet:not([hidden])'));
  });
})();

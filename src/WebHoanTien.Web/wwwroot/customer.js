(() => {
  const linkForm = document.getElementById('link-form');
  const createButton = document.getElementById('create-button');
  const linkInput = document.getElementById('affiliate-url');
  const clearButton = document.getElementById('affiliate-url-clear');
  const urlStatus = document.getElementById('url-status');
  const visibilityStatus = document.querySelector('[data-link-visibility-status]');
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

  const showVisibilityStatus = (message, state = 'success') => {
    if (!visibilityStatus) return;
    visibilityStatus.textContent = message;
    visibilityStatus.dataset.state = state;
    visibilityStatus.hidden = false;
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
      if (actionInput) actionInput.value = isHidden ? 'show' : 'hide';
      if (actionLabel) {
        const isDesktop = actionLabel.closest('.affiliate-hide-desktop-button');
        actionLabel.textContent = isHidden
          ? (isDesktop ? 'Bỏ ẩn' : 'Bỏ ẩn khỏi danh sách')
          : (isDesktop ? 'Ẩn link' : 'Ẩn khỏi danh sách');
      }
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

    const moreButton = card.querySelector('[data-link-actions-open]');
    if (moreButton) {
      moreButton.dataset.linkActionsOpen = actionSheetId;
      moreButton.setAttribute('aria-label', `Mở thao tác cho ${productName}`);
    }

    const clickCount = card.querySelector('[data-click-count]') || card.querySelector('.affiliate-link-desktop-footer > span');
    if (clickCount) clickCount.textContent = `Shopee · ${link.clickCount || 0} lượt bấm`;

    if (sheet) {
      sheet.id = actionSheetId;
      sheet.hidden = true;
      sheet.setAttribute('aria-hidden', 'true');
    }

    [card, sheet].filter(Boolean).forEach((root) => {
      root.querySelectorAll('[data-copy-url]').forEach((button) => {
        button.dataset.copyUrl = link.affiliateUrl || '';
        button.hidden = !link.affiliateUrl;
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
      showVisibilityStatus(result.message);
      if (result.isHidden) {
        card?.classList.add('is-removing');
        window.setTimeout(() => {
          card?.remove();
          sheet?.remove();
          renderEmptyLinkState();
        }, 210);
      } else {
        updateCardVisibility(card, linkId, result.isHidden ?? requestedHidden);
      }
    } catch (error) {
      showVisibilityStatus(error.message || 'Không thể cập nhật link lúc này. Vui lòng thử lại.', 'error');
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
    const label = copyButton.querySelector('span') || copyButton;
    const originalText = label.textContent;
    try {
      await navigator.clipboard.writeText(copyButton.dataset.copyUrl);
      label.textContent = 'Đã sao chép link';
      if (copyButton.hasAttribute('data-link-actions-close-after-copy')) {
        window.setTimeout(() => closeActionSheet(copyButton.closest('.affiliate-action-sheet')), 450);
      }
      window.setTimeout(() => { label.textContent = originalText; }, 1800);
    } catch {
      label.textContent = 'Không thể sao chép';
    }
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') closeActionSheet(document.querySelector('.affiliate-action-sheet:not([hidden])'));
  });
})();

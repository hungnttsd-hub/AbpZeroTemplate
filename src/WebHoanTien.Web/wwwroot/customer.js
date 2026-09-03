window.CatBackSpa.mount('customer-dashboard', ({ signal, visit }) => {
  const linkForm = document.getElementById('link-form');
  const createButton = document.getElementById('create-button');
  const linkInput = document.getElementById('affiliate-url');
  const clearButton = document.getElementById('affiliate-url-clear');
  const urlStatus = document.getElementById('url-status');
  const urlLabel = document.getElementById('affiliate-url-label');
  const targetInputs = Array.from(linkForm?.querySelectorAll('input[name="LinkTargetType"]') || []);
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

  const selectedTargetType = () => targetInputs.find((input) => input.checked)?.value || 'Product';

  const clearInlineResult = () => {
    linkForm?.querySelector('[data-affiliate-inline-result]')?.remove();
    if (createButton) createButton.hidden = false;
  };

  const syncTargetMode = (resetState = false) => {
    const isShop = selectedTargetType() === 'Shop';
    if (linkInput) linkInput.placeholder = isShop
      ? 'Dán link cửa hàng Shopee tại đây...'
      : 'Dán link sản phẩm Shopee tại đây...';
    if (urlLabel) urlLabel.textContent = isShop ? 'Link cửa hàng Shopee' : 'Link sản phẩm Shopee';
    if (resetState) {
      clearInlineResult();
      showUrlStatus('', 'idle');
    }
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
      (details.querySelector('.affiliate-link-meta') || details.querySelector('.affiliate-store') ||
        details.querySelector('h3'))?.insertAdjacentElement('afterend', badge);
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

  const setLinkImage = (card, imageUrl, title, targetType) => {
    const imageBox = card.querySelector('.affiliate-product-image');
    if (!imageBox) return;
    let image = Array.from(imageBox.children).find((element) => element.tagName === 'IMG');
    let placeholder = Array.from(imageBox.children).find((element) => element.tagName === 'SPAN');
    const isProduct = targetType === 'Product';
    const supportsRemoteImage = isProduct || targetType === 'Shop';

    if (supportsRemoteImage && imageUrl) {
      if (!image) {
        image = document.createElement('img');
        imageBox.prepend(image);
      }
      image.src = imageUrl;
      image.alt = `Ảnh ${title}`;
      image.loading = 'lazy';
      image.onerror = () => {
        image.onerror = null;
        image.src = targetType === 'Shop'
          ? '/catback/icons/shop-placeholder.svg'
          : '/catback/icons/shopping-bag.svg';
      };
      image.hidden = false;
      if (placeholder) placeholder.hidden = true;
      return;
    }

    if (image) image.hidden = true;
    if (!placeholder) {
      placeholder = document.createElement('span');
      const placeholderImage = document.createElement('img');
      placeholderImage.alt = '';
      placeholder.append(placeholderImage);
      imageBox.append(placeholder);
    }
    const placeholderImage = placeholder.querySelector('img') || document.createElement('img');
    placeholderImage.src = targetType === 'Shop'
      ? '/catback/icons/shop-placeholder.svg'
      : '/catback/icons/shopping-bag.svg';
    placeholderImage.alt = '';
    if (!placeholderImage.isConnected) placeholder.append(placeholderImage);
    placeholder.hidden = false;
  };

  const setLinkCardData = (card, sheet, link) => {
    const linkId = String(link.id);
    const actionSheetId = getActionSheetId(linkId);
    const targetType = link.targetType || 'Product';
    const isShop = targetType === 'Shop';
    const title = isShop
      ? (link.productName || (link.shopId ? `Shop #${link.shopId}` : 'Cửa hàng Shopee'))
      : (link.productName || 'Sản phẩm Shopee');
    card.dataset.linkId = linkId;
    card.dataset.targetType = targetType;
    card.classList.remove('is-hidden', 'is-removing');
    card.classList.toggle('is-shop-link', isShop);
    card.classList.remove('is-legacy-link');
    card.classList.add('is-new');

    setLinkImage(card, link.imageUrl, title, targetType);
    const productNameElement = card.querySelector('.affiliate-link-details h3');
    if (productNameElement) productNameElement.textContent = title;

    const details = card.querySelector('.affiliate-link-details');
    let store = details?.querySelector('.affiliate-store');
    let targetBadge = details?.querySelector('.affiliate-target-badge');
    let linkMeta = details?.querySelector('.affiliate-link-meta');
    if (!store && details) {
      store = document.createElement('p');
      store.className = 'affiliate-store';
      details.querySelector('h3')?.insertAdjacentElement('afterend', store);
    }
    if (!targetBadge && details) {
      targetBadge = document.createElement('span');
      targetBadge.className = 'affiliate-target-badge';
      store?.insertAdjacentElement('afterend', targetBadge);
    }
    if (!linkMeta && details) {
      linkMeta = document.createElement('p');
      linkMeta.className = 'affiliate-link-meta';
      targetBadge?.insertAdjacentElement('afterend', linkMeta);
    }
    if (store) {
      store.textContent = 'Sản phẩm từ Shopee';
      store.hidden = isShop;
    }
    if (targetBadge) {
      targetBadge.textContent = 'Link cửa hàng';
      targetBadge.hidden = !isShop;
    }
    if (linkMeta) {
      linkMeta.textContent = 'Mua nhiều sản phẩm trong cùng shop';
      linkMeta.hidden = !isShop;
    }

    const estimate = card.querySelector('.affiliate-estimate');
    if (estimate) {
      estimate.hidden = isShop;
      estimate.textContent = link.estimatedCommissionLabel
        ? `Hoàn lại dự kiến ${link.estimatedCommissionLabel}`
        : 'Chưa có ước tính hoa hồng';
      estimate.classList.toggle('unavailable', !link.estimatedCommissionLabel);
    }

    const buyButton = card.querySelector('.affiliate-buy-button');
    if (buyButton) {
      buyButton.href = link.redirectUrl;
      buyButton.textContent = isShop ? 'Vào Shop' : 'Mua ngay';
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

  const renderInlineResult = (link) => {
    if (!linkForm || !link?.redirectUrl) return;
    clearInlineResult();
    const template = document.getElementById('affiliate-inline-result-template');
    if (!template) return;
    const fragment = template.content.cloneNode(true);
    const result = fragment.querySelector('[data-affiliate-inline-result]');
    const isShop = link.targetType === 'Shop';
    const fallbackImage = isShop
      ? '/catback/icons/shop-placeholder.svg'
      : '/catback/icons/shopping-bag.svg';
    const title = isShop
      ? (link.productName || (link.shopId ? `Shop #${link.shopId}` : 'Cửa hàng Shopee'))
      : (link.productName || 'Sản phẩm Shopee');
    result?.classList.add(isShop ? 'is-shop' : 'is-product');
    const titleElement = result?.querySelector('[data-affiliate-title]');
    if (titleElement) titleElement.textContent = title;
    const targetBadge = result?.querySelector('[data-affiliate-target-badge]');
    if (targetBadge) targetBadge.textContent = isShop ? 'Link cửa hàng' : 'Link sản phẩm';
    const resultImage = result?.querySelector('[data-affiliate-image]');
    if (resultImage) {
      resultImage.src = link.imageUrl || fallbackImage;
      resultImage.alt = `Ảnh ${title}`;
      resultImage.onerror = () => {
        resultImage.onerror = null;
        resultImage.src = fallbackImage;
      };
    }
    const stableUrl = new URL(link.redirectUrl, window.location.origin).href;
    const stableUrlElement = result?.querySelector('[data-affiliate-stable-url]');
    if (stableUrlElement) stableUrlElement.textContent = stableUrl.replace(/^https?:\/\//, '');
    const copyButton = result?.querySelector('[data-copy-url]');
    if (copyButton) copyButton.dataset.copyUrl = link.redirectUrl;
    const buyLabel = result?.querySelector('[data-affiliate-buy-label]');
    if (buyLabel) buyLabel.textContent = isShop ? 'Vào Shop mua hàng' : 'Mua ngay';
    const hint = result?.querySelector('[data-affiliate-hint]');
    if (hint) hint.textContent = isShop
      ? 'Mở shop trên Shopee để thêm nhiều sản phẩm vào giỏ hàng trong cùng một lần mua.'
      : 'Mở sản phẩm trên Shopee và hoàn tất đơn trong cùng phiên để được ghi nhận hoàn tiền.';
    const buyButton = result?.querySelector('.affiliate-inline-buy-button');
    if (buyButton) {
      buyButton.href = link.redirectUrl;
      if (window.matchMedia('(min-width: 768px)').matches) buyButton.target = '_blank';
    }
    linkForm.insertBefore(fragment, createButton);
    if (createButton) createButton.hidden = true;
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
        card?.addEventListener('animationend', finishRemoval, { once: true, signal });
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

  targetInputs.forEach((input) => input.addEventListener('change', () => syncTargetMode(true), { signal }));
  linkInput?.addEventListener('input', () => {
    syncClearButton();
    if (linkForm?.querySelector('[data-affiliate-inline-result]')) clearInlineResult();
    if (urlStatus?.dataset.state !== 'idle') showUrlStatus('', 'idle');
  }, { signal });
  clearButton?.addEventListener('click', () => {
    linkInput.value = '';
    clearInlineResult();
    showUrlStatus('', 'idle');
    syncClearButton();
    linkInput.focus();
  }, { signal });
  syncClearButton();
  syncTargetMode(false);

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
        visit(result.redirectUrl);
        return;
      }

      upsertLinkCard(result.link);
      renderInlineResult(result.link);
      syncClearButton();
      showUrlStatus(result.message, 'success');
    } catch (error) {
      showUrlStatus(error.message || 'Không thể tạo link mua hàng lúc này. Vui lòng thử lại sau.', 'error');
    } finally {
      createButton.disabled = false;
      if (createButtonContent !== undefined) createButton.innerHTML = createButtonContent;
    }
  }, { signal });

  document.addEventListener('submit', (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.matches('[data-link-visibility-form]')) return;
    event.preventDefault();
    void submitLinkVisibility(form);
  }, { signal });

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
  }, { signal });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') closeActionSheet(document.querySelector('.affiliate-action-sheet:not([hidden])'));
  }, { signal });
});

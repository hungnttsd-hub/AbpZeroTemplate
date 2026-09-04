window.CatBackSpa.mount('infinite-scroll', ({ signal, visit }) => {
  const loaders = Array.from(document.querySelectorAll('[data-infinite-scroll]'));
  const observers = [];

  const itemKey = (item) => item.dataset.linkId || item.dataset.orderId || '';

  const setupLoader = (loader) => {
    const list = document.querySelector(loader.dataset.listSelector || '');
    const retryButton = loader.querySelector('[data-loader-retry]');
    const message = loader.querySelector('[data-loader-message]');
    const itemSelector = loader.dataset.itemSelector || ':scope > *';
    const itemName = loader.dataset.itemName || 'mục';
    const loadingMessage = loader.dataset.loadingMessage || 'Đang tải thêm...';
    let loading = false;
    let observer;
    let skeleton;

    if (!list) return;

    const finish = () => {
      const totalCount = Math.max(0, Number.parseInt(loader.dataset.totalCount || '0', 10) || 0);
      loader.dataset.hasMore = 'false';
      loader.classList.remove('has-error', 'is-loading');
      loader.classList.add('is-complete');
      if (message) message.textContent = `Đã hiển thị tất cả ${totalCount} ${itemName}.`;
      if (retryButton) retryButton.hidden = true;
      observer?.disconnect();
    };

    if (loader.dataset.hasMore === 'false') {
      finish();
      return;
    }

    const loadNext = async () => {
      if (loading || loader.dataset.hasMore === 'false') return;
      loading = true;
      loader.classList.remove('has-error', 'is-complete');
      loader.classList.add('is-loading');
      loader.setAttribute('aria-busy', 'true');
      list.setAttribute('aria-busy', 'true');
      if (message) message.textContent = loadingMessage;
      if (retryButton) retryButton.hidden = true;
      skeleton = window.CatBackLoading?.showSkeleton(list, { rows: 3 });

      try {
        const nextSkip = Math.max(0, Number.parseInt(loader.dataset.nextSkip || '0', 10) || 0);
        const pageSize = Math.max(1, Number.parseInt(loader.dataset.pageSize || '10', 10) || 10);
        const url = new URL(loader.dataset.url || window.location.href, window.location.origin);
        url.searchParams.set('skip', String(nextSkip));
        const response = await fetch(url, {
          credentials: 'same-origin',
          headers: { 'X-Requested-With': 'XMLHttpRequest', Accept: 'text/html' },
          signal
        });

        if (response.redirected && new URL(response.url).pathname.startsWith('/Account/')) {
          visit(response.url);
          return;
        }
        if (!response.ok) throw new Error('Không thể tải thêm dữ liệu.');

        const template = document.createElement('template');
        template.innerHTML = await response.text();
        const fetchedItems = Array.from(template.content.querySelectorAll(itemSelector));
        const hasMore = response.headers.get('X-Has-More') === 'true';
        const responseTotal = Number.parseInt(response.headers.get('X-Total-Count') || '', 10);
        if (Number.isFinite(responseTotal)) loader.dataset.totalCount = String(responseTotal);
        if (hasMore && fetchedItems.length === 0) throw new Error('Trang dữ liệu trả về rỗng.');

        const existingKeys = new Set(Array.from(list.querySelectorAll(itemSelector)).map(itemKey).filter(Boolean));
        fetchedItems.forEach((item) => {
          const key = itemKey(item);
          if (key && existingKeys.has(key)) return;
          if (key) existingKeys.add(key);
          item.classList.add('is-infinite-new');
          list.append(item);
        });

        const loadedCount = nextSkip + fetchedItems.length;
        loader.dataset.nextSkip = String(loadedCount);
        loader.dataset.hasMore = String(hasMore);
        if (!hasMore) {
          finish();
        } else if (message) {
          const totalCount = Number.parseInt(loader.dataset.totalCount || '', 10);
          message.textContent = Number.isFinite(totalCount)
            ? `Đã tải ${loadedCount}/${totalCount} ${itemName}. Kéo xuống để tải tiếp.`
            : `Đã tải thêm ${fetchedItems.length} ${itemName}. Kéo xuống để tải tiếp.`;
        }
      } catch (error) {
        if (error?.name === 'AbortError') return;
        loader.classList.add('has-error');
        if (message) message.textContent = 'Tải dữ liệu chưa thành công.';
        if (retryButton) retryButton.hidden = false;
      } finally {
        loading = false;
        loader.classList.remove('is-loading');
        loader.setAttribute('aria-busy', 'false');
        window.CatBackLoading?.hideSkeleton(list, skeleton);
        skeleton = undefined;
        if (!list.querySelector(':scope > .cb-skeleton-list')) list.removeAttribute('aria-busy');
      }
    };

    retryButton?.addEventListener('click', () => {
      loader.classList.remove('has-error');
      void loadNext();
    }, { signal });

    if ('IntersectionObserver' in window) {
      observer = new IntersectionObserver((entries) => {
        if (entries.some((entry) => entry.isIntersecting)) void loadNext();
      }, { rootMargin: '120px 0px' });
      observer.observe(loader);
      observers.push(observer);
    } else if (retryButton) {
      if (message) message.textContent = 'Còn dữ liệu chưa hiển thị.';
      retryButton.textContent = 'Tải thêm';
      retryButton.hidden = false;
    }
  };

  loaders.forEach(setupLoader);
  return () => observers.forEach((observer) => observer.disconnect());
});

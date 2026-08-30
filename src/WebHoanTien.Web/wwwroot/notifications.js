(function () {
    const root = document.querySelector('[data-notification-root]');
    if (!root) return;

    const listHost = root.querySelector('[data-notification-list]');
    const emptyState = root.querySelector('[data-notification-empty]');
    const loadMoreButton = root.querySelector('[data-notification-load-more]');
    const filterSheet = document.querySelector('[data-notification-filter-sheet]');
    const filterBackdrop = document.querySelector('[data-notification-filter-backdrop]');
    const unreadInput = filterSheet.querySelector('[data-sheet-unread]');
    const token = document.querySelector('[data-notification-antiforgery] input[name="__RequestVerificationToken"]')?.value || '';
    const toast = document.querySelector('[data-notification-toast]');
    const backButton = document.querySelector('[data-notification-back]');
    let category = root.dataset.category || '';
    let unreadOnly = root.dataset.unreadOnly === 'true';
    let totalCount = Number(root.dataset.totalCount || 0);
    let isBusy = false;

    const iconByKind = {
        1: 'pending.svg', 2: 'cashback.svg', 10: 'order.svg', 11: 'status-negative.svg',
        12: 'status-negative.svg', 13: 'status-negative.svg', 20: 'wallet.svg', 21: 'wallet.svg',
        22: 'status-negative.svg', 23: 'status-negative.svg', 24: 'bank.svg', 30: 'promotion.svg',
        40: 'bell.svg'
    };
    const toneByKind = {
        1: 'pending', 2: 'cashback', 10: 'order', 11: 'negative', 12: 'negative', 13: 'negative',
        20: 'wallet', 21: 'cashback', 22: 'negative', 23: 'negative', 24: 'bank', 30: 'promotion',
        40: 'order'
    };

    function showToast(message) {
        toast.textContent = message;
        toast.hidden = false;
        window.clearTimeout(showToast.timer);
        showToast.timer = window.setTimeout(() => { toast.hidden = true; }, 2800);
    }

    backButton?.addEventListener('click', (event) => {
        if (!document.referrer) return;
        try {
            if (new URL(document.referrer).origin !== window.location.origin) return;
            event.preventDefault();
            window.history.back();
        } catch {
            // Keep the home-page fallback from the anchor.
        }
    });

    function updateBadges(count) {
        document.querySelectorAll('[data-notification-badge]').forEach((badge) => {
            badge.textContent = count > 99 ? '99+' : String(count);
            badge.hidden = count <= 0;
        });
    }

    function formatTime(value) {
        const date = new Date(value);
        const now = new Date();
        const elapsedMinutes = Math.max(0, Math.floor((now - date) / 60000));
        if (elapsedMinutes < 1) return 'Vừa xong';
        if (elapsedMinutes < 60) return `${elapsedMinutes} phút trước`;
        const sameDay = date.toDateString() === now.toDateString();
        if (sameDay && elapsedMinutes < 1440) return `${Math.floor(elapsedMinutes / 60)} giờ trước`;
        const yesterday = new Date(now); yesterday.setDate(now.getDate() - 1);
        if (date.toDateString() === yesterday.toDateString()) return 'Hôm qua';
        return date.toLocaleDateString('vi-VN');
    }

    function createCard(item) {
        const card = document.createElement('a');
        card.className = `notification-card${item.isRead ? '' : ' is-unread'}`;
        card.href = `/Notifications/${item.id}`;
        card.dataset.notificationId = item.id;
        card.dataset.notificationKind = item.kind;

        const icon = document.createElement('span');
        icon.className = `notification-card-icon notification-tone-${toneByKind[item.kind] || 'wallet'}`;
        icon.setAttribute('aria-hidden', 'true');
        const image = document.createElement('img');
        image.src = `/notification-icons/${iconByKind[item.kind] || 'wallet.svg'}`;
        image.alt = '';
        icon.appendChild(image);

        const copy = document.createElement('span');
        copy.className = 'notification-card-copy';
        const title = document.createElement('strong');
        title.textContent = item.title;
        const message = document.createElement('span');
        message.textContent = item.message;
        copy.append(title, message);

        const time = document.createElement('time');
        time.dateTime = item.creationTime;
        time.textContent = formatTime(item.creationTime);
        const chevron = document.createElement('span');
        chevron.className = 'notification-card-chevron';
        chevron.setAttribute('aria-hidden', 'true');
        const chevronImage = document.createElement('img');
        chevronImage.src = '/notification-icons/chevron-right.svg';
        chevronImage.alt = '';
        chevron.appendChild(chevronImage);
        const dot = document.createElement('span');
        dot.className = 'notification-unread-dot';
        dot.setAttribute('aria-label', 'Chưa đọc');
        card.append(icon, copy, time, chevron, dot);
        return card;
    }

    function isToday(value) {
        return new Date(value).toDateString() === new Date().toDateString();
    }

    function createGroup(name) {
        const section = document.createElement('section');
        section.className = 'notification-group';
        section.dataset.notificationGroup = name;
        const heading = document.createElement('header');
        heading.className = `notification-group-heading${name === 'previous' ? ' notification-previous-heading' : ''}`;
        const title = document.createElement('h2');
        title.textContent = name === 'today' ? 'Hôm nay' : 'Trước đó';
        const cards = document.createElement('div');
        cards.className = 'notification-card-list';
        heading.appendChild(title);
        section.append(heading, cards);
        return section;
    }

    function createReadAllButton() {
        const button = document.createElement('button');
        button.type = 'button';
        button.dataset.markAllRead = '';
        const image = document.createElement('img');
        image.src = '/notification-icons/mark-read.svg';
        image.alt = '';
        button.append(image, document.createTextNode('Đánh dấu đã đọc tất cả'));
        return button;
    }

    function normalizeGroupHeadings() {
        listHost.querySelectorAll('[data-mark-all-read]').forEach((button) => button.remove());
        const firstHeading = listHost.querySelector('.notification-group-heading');
        if (firstHeading) firstHeading.appendChild(createReadAllButton());
    }

    function appendItems(items) {
        items.forEach((item) => {
            const groupName = isToday(item.creationTime) ? 'today' : 'previous';
            let group = listHost.querySelector(`[data-notification-group="${groupName}"]`);
            if (!group) {
                group = createGroup(groupName);
                if (groupName === 'today') listHost.prepend(group); else listHost.append(group);
            }
            group.querySelector('.notification-card-list').appendChild(createCard(item));
        });
        normalizeGroupHeadings();
    }

    function renderItems(items) {
        listHost.replaceChildren();
        appendItems(items);
        emptyState.hidden = items.length > 0;
    }

    async function getNotifications(skipCount) {
        const query = new URLSearchParams({ handler: 'List', unreadOnly: String(unreadOnly), skipCount: String(skipCount) });
        if (category) query.set('category', category);
        const response = await fetch(`/Notifications?${query}`, { headers: { Accept: 'application/json' }, credentials: 'same-origin' });
        if (!response.ok) throw new Error('Không thể tải danh sách thông báo.');
        return response.json();
    }

    function updateUrl() {
        const query = new URLSearchParams();
        if (category) query.set('category', category);
        if (unreadOnly) query.set('unreadOnly', 'true');
        history.replaceState(null, '', `/Notifications${query.size ? `?${query}` : ''}`);
    }

    function syncFilters() {
        root.querySelectorAll('[data-notification-category]').forEach((button) => {
            button.classList.toggle('is-active', button.dataset.notificationCategory === category);
        });
        filterSheet.querySelectorAll('[data-sheet-category]').forEach((button) => {
            button.classList.toggle('is-active', button.dataset.sheetCategory === category);
        });
        unreadInput.checked = unreadOnly;
    }

    async function reloadList() {
        if (isBusy) return;
        isBusy = true;
        loadMoreButton.disabled = true;
        try {
            const data = await getNotifications(0);
            totalCount = data.totalCount;
            renderItems(data.items);
            updateBadges(data.unreadCount);
            loadMoreButton.hidden = data.items.length >= totalCount;
            updateUrl();
        } catch (error) {
            showToast(error.message);
        } finally {
            isBusy = false;
            loadMoreButton.disabled = false;
        }
    }

    async function postHandler(handler, values) {
        const response = await fetch(`/Notifications?handler=${handler}`, {
            method: 'POST', credentials: 'same-origin',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8', RequestVerificationToken: token },
            body: new URLSearchParams(values)
        });
        const data = await response.json();
        if (!response.ok || data.success === false) throw new Error(data.error || 'Không thể thực hiện thao tác.');
        return data;
    }

    function openFilter() {
        syncFilters();
        filterSheet.hidden = false;
        filterBackdrop.hidden = false;
        document.body.style.overflow = 'hidden';
    }

    function closeFilter() {
        filterSheet.hidden = true;
        filterBackdrop.hidden = true;
        document.body.style.overflow = '';
    }

    root.addEventListener('click', async (event) => {
        const categoryButton = event.target.closest('[data-notification-category]');
        if (categoryButton) {
            category = categoryButton.dataset.notificationCategory || '';
            syncFilters();
            await reloadList();
            return;
        }

        const readAll = event.target.closest('[data-mark-all-read]');
        if (readAll) {
            readAll.disabled = true;
            try {
                await postHandler('ReadAll', {});
                listHost.querySelectorAll('.notification-card.is-unread').forEach((card) => card.classList.remove('is-unread'));
                updateBadges(0);
                if (unreadOnly) await reloadList();
                else showToast('Đã đánh dấu tất cả thông báo là đã đọc.');
            } catch (error) {
                showToast(error.message);
            } finally {
                readAll.disabled = false;
            }
            return;
        }

        const card = event.target.closest('[data-notification-id]');
        if (card) {
            event.preventDefault();
            if (card.dataset.opening === 'true') return;
            card.dataset.opening = 'true';
            try {
                const data = await postHandler('Read', { notificationId: card.dataset.notificationId });
                card.classList.remove('is-unread');
                updateBadges(data.unreadCount);
                window.location.assign(card.href);
            } catch (error) {
                delete card.dataset.opening;
                showToast(error.message);
            }
        }
    });

    loadMoreButton.addEventListener('click', async () => {
        if (isBusy) return;
        isBusy = true;
        loadMoreButton.disabled = true;
        try {
            const currentCount = listHost.querySelectorAll('[data-notification-id]').length;
            const data = await getNotifications(currentCount);
            totalCount = data.totalCount;
            appendItems(data.items);
            emptyState.hidden = currentCount + data.items.length > 0;
            loadMoreButton.hidden = currentCount + data.items.length >= totalCount;
            updateBadges(data.unreadCount);
        } catch (error) {
            showToast(error.message);
        } finally {
            isBusy = false;
            loadMoreButton.disabled = false;
        }
    });

    document.querySelector('[data-open-notification-filter]').addEventListener('click', openFilter);
    document.querySelector('[data-close-notification-filter]').addEventListener('click', closeFilter);
    filterBackdrop.addEventListener('click', closeFilter);
    filterSheet.addEventListener('click', (event) => {
        const option = event.target.closest('[data-sheet-category]');
        if (!option) return;
        filterSheet.querySelectorAll('[data-sheet-category]').forEach((button) => button.classList.remove('is-active'));
        option.classList.add('is-active');
    });
    document.querySelector('[data-apply-notification-filter]').addEventListener('click', async () => {
        category = filterSheet.querySelector('[data-sheet-category].is-active')?.dataset.sheetCategory || '';
        unreadOnly = unreadInput.checked;
        closeFilter();
        syncFilters();
        await reloadList();
    });
    syncFilters();
})();

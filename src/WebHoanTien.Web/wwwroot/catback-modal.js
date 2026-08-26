(function () {
    'use strict';

    let elements;
    let activeRequest;
    let previousFocus;

    function createElement(tagName, className) {
        const element = document.createElement(tagName);
        if (className) element.className = className;
        return element;
    }

    function ensureElements() {
        if (elements) return elements;

        const root = createElement('div', 'cb-modal-root');
        root.hidden = true;
        root.setAttribute('aria-hidden', 'true');

        const overlay = createElement('div', 'cb-modal-overlay');
        const dialog = createElement('section', 'cb-modal-dialog');
        dialog.setAttribute('role', 'dialog');
        dialog.setAttribute('aria-modal', 'true');
        dialog.setAttribute('aria-labelledby', 'cb-modal-title');
        dialog.setAttribute('aria-describedby', 'cb-modal-message');
        dialog.tabIndex = -1;

        const close = createElement('button', 'cb-modal-close');
        close.type = 'button';
        close.setAttribute('aria-label', 'Đóng');
        const closeIcon = document.createElement('img');
        closeIcon.src = '/catback-modal-icons/close.svg';
        closeIcon.alt = '';
        close.appendChild(closeIcon);

        const icon = createElement('span', 'cb-modal-icon');
        icon.setAttribute('aria-hidden', 'true');
        const iconImage = document.createElement('img');
        iconImage.alt = '';
        icon.appendChild(iconImage);

        const title = createElement('h2', 'cb-modal-title');
        title.id = 'cb-modal-title';
        const message = createElement('p', 'cb-modal-message');
        message.id = 'cb-modal-message';

        const actions = createElement('div', 'cb-modal-actions');
        const cancel = createElement('button', 'cb-modal-button cb-modal-button-secondary');
        cancel.type = 'button';
        const confirm = createElement('button', 'cb-modal-button cb-modal-button-primary');
        confirm.type = 'button';
        actions.append(cancel, confirm);
        dialog.append(close, icon, title, message, actions);
        root.append(overlay, dialog);
        document.body.appendChild(root);

        elements = { root, overlay, dialog, close, iconImage, title, message, actions, cancel, confirm };
        close.addEventListener('click', () => finish(false));
        cancel.addEventListener('click', () => finish(false));
        confirm.addEventListener('click', () => finish(true));
        overlay.addEventListener('click', () => {
            if (activeRequest?.closeOnBackdrop) finish(false);
        });
        dialog.addEventListener('keydown', trapFocus);
        document.addEventListener('keydown', onDocumentKeyDown);
        return elements;
    }

    function focusableElements() {
        return [elements.close, elements.cancel, elements.confirm].filter(element => !element.hidden && !element.disabled);
    }

    function trapFocus(event) {
        if (event.key !== 'Tab') return;
        const focusable = focusableElements();
        if (focusable.length === 0) return;
        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    }

    function onDocumentKeyDown(event) {
        if (!activeRequest || event.key !== 'Escape' || activeRequest.dismissible === false) return;
        event.preventDefault();
        finish(false);
    }

    function finish(result) {
        if (!activeRequest) return;
        const request = activeRequest;
        activeRequest = undefined;
        elements.root.hidden = true;
        elements.root.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('cb-modal-open');
        previousFocus?.focus?.();
        request.resolve(result);
    }

    function show(options) {
        ensureElements();
        if (activeRequest) finish(false);

        const variant = ['warning', 'success', 'info'].includes(options.variant) ? options.variant : 'info';
        const showCancel = options.showCancel === true;
        previousFocus = document.activeElement;
        elements.dialog.dataset.variant = variant;
        elements.iconImage.src = `/catback-modal-icons/${variant}.svg`;
        elements.title.textContent = options.title || 'Thông báo';
        elements.message.textContent = options.message || '';
        elements.message.hidden = !options.message;
        elements.cancel.textContent = options.cancelText || 'Hủy';
        elements.cancel.hidden = !showCancel;
        elements.confirm.textContent = options.confirmText || (variant === 'info' ? 'Đã hiểu' : 'Đóng');
        elements.actions.classList.toggle('is-single', !showCancel);
        elements.close.hidden = options.showClose === false;
        elements.root.hidden = false;
        elements.root.setAttribute('aria-hidden', 'false');
        document.body.classList.add('cb-modal-open');

        return new Promise(resolve => {
            activeRequest = {
                resolve,
                closeOnBackdrop: options.closeOnBackdrop === true,
                dismissible: options.dismissible !== false
            };
            window.requestAnimationFrame(() => elements.confirm.focus());
        });
    }

    window.CatsBackModal = {
        confirm(options) {
            return show({
                variant: options?.variant || 'warning',
                title: options?.title || 'Bạn có chắc chắn?',
                message: options?.message || '',
                cancelText: options?.cancelText || 'Hủy',
                confirmText: options?.confirmText || 'Xác nhận',
                showCancel: true,
                showClose: options?.showClose !== false,
                closeOnBackdrop: false,
                dismissible: true
            });
        },
        success(options) {
            return show({
                variant: 'success',
                title: options?.title || 'Đã hoàn tất!',
                message: options?.message || '',
                confirmText: options?.confirmText || 'Đóng',
                showCancel: false,
                showClose: options?.showClose !== false,
                closeOnBackdrop: true,
                dismissible: true
            });
        },
        info(options) {
            return show({
                variant: 'info',
                title: options?.title || 'Thông tin',
                message: options?.message || '',
                confirmText: options?.confirmText || 'Đã hiểu',
                showCancel: false,
                showClose: options?.showClose !== false,
                closeOnBackdrop: true,
                dismissible: true
            });
        }
    };
})();

window.CatBackSpa.mount('customer-profile', ({ signal }) => {
    const forms = document.querySelectorAll('[data-profile-ajax-form]');
    if (forms.length === 0) return;

    function clearValidation(form) {
        form.querySelectorAll('[data-valmsg-for]').forEach(element => {
            element.textContent = '';
        });
        form.querySelectorAll('[data-valmsg-summary="true"]').forEach(element => {
            element.replaceChildren();
        });
        Array.from(form.elements).forEach(element => {
            if (element instanceof HTMLElement) element.removeAttribute('aria-invalid');
        });
    }

    function findValidationMessage(form, fieldName) {
        return Array.from(form.querySelectorAll('[data-valmsg-for]'))
            .find(element => element.getAttribute('data-valmsg-for') === fieldName);
    }

    function findField(form, fieldName) {
        return Array.from(form.elements)
            .find(element => element instanceof HTMLElement && element.getAttribute('name') === fieldName);
    }

    function renderValidation(form, errors) {
        let firstInvalidField;
        const summaryMessages = [];
        Object.entries(errors || {}).forEach(([fieldName, messages]) => {
            const normalizedMessages = Array.isArray(messages) ? messages.filter(Boolean) : [];
            if (normalizedMessages.length === 0) return;

            if (!fieldName) {
                summaryMessages.push(...normalizedMessages);
                return;
            }

            const messageElement = findValidationMessage(form, fieldName);
            if (messageElement) messageElement.textContent = normalizedMessages.join(' ');
            const field = findField(form, fieldName);
            if (field) {
                field.setAttribute('aria-invalid', 'true');
                firstInvalidField ||= field;
            }
        });

        if (summaryMessages.length > 0) {
            const summary = form.querySelector('[data-valmsg-summary="true"]');
            if (summary) {
                const list = document.createElement('ul');
                summaryMessages.forEach(message => {
                    const item = document.createElement('li');
                    item.textContent = message;
                    list.appendChild(item);
                });
                summary.replaceChildren(list);
            }
        }

        firstInvalidField?.focus();
    }

    async function readResponse(response) {
        let payload = {};
        try {
            payload = await response.json();
        } catch (_) {
            // The fallback message below also covers expired sessions and non-JSON server errors.
        }

        if (!response.ok || payload.success !== true) {
            let responseError = 'Không thể lưu thông tin lúc này. Vui lòng thử lại.';
            if (typeof payload.error?.message === 'string') responseError = payload.error.message;
            else if (typeof payload.error === 'string') responseError = payload.error;
            else if (typeof payload.message === 'string') responseError = payload.message;
            else if (response.redirected) {
                responseError = 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại rồi thử lại.';
            }

            const error = new Error(responseError);
            error.validationErrors = payload.errors || {};
            throw error;
        }

        return payload;
    }

    function showResult(type, options) {
        const modal = window.CatsBackModal;
        if (modal) {
            const show = type === 'success'
                ? modal.success
                : modal.warning || modal.info;
            if (typeof show === 'function') {
                void show.call(modal, options);
                return;
            }
        }

        window.alert(options.message || options.title);
    }

    function syncSavedValues(form, payload) {
        if (payload.contactEmail) {
            const contactEmail = findField(form, 'ContactEmail');
            if (contactEmail) contactEmail.value = payload.contactEmail;
        }

        if (payload.payoutAccount) {
            const values = {
                'PayoutInput.BankCode': payload.payoutAccount.bankCode,
                'PayoutInput.AccountNumber': payload.payoutAccount.accountNumber,
                'PayoutInput.AccountHolderName': payload.payoutAccount.accountHolderName
            };
            Object.entries(values).forEach(([fieldName, value]) => {
                const field = findField(form, fieldName);
                if (field && typeof value === 'string') field.value = value;
            });
        }

        if (Number.isInteger(payload.unreadNotificationCount)) {
            document.querySelectorAll('[data-notification-badge]').forEach(badge => {
                badge.textContent = payload.unreadNotificationCount > 99
                    ? '99+'
                    : String(payload.unreadNotificationCount);
                badge.hidden = payload.unreadNotificationCount <= 0;
            });
            document.querySelectorAll('.header-notification-bell').forEach(bell => {
                bell.setAttribute(
                    'aria-label',
                    `Mở thông báo, ${payload.unreadNotificationCount} chưa đọc`
                );
            });
        }
    }

    forms.forEach(form => {
        form.addEventListener('submit', async event => {
            event.preventDefault();
            if (form.dataset.submitting === 'true') return;
            if (!form.checkValidity()) {
                form.reportValidity();
                return;
            }

            clearValidation(form);
            const submitButton = form.querySelector('button[type="submit"]');
            const originalButtonHtml = submitButton?.innerHTML || '';
            form.dataset.submitting = 'true';
            form.setAttribute('aria-busy', 'true');
            if (submitButton) {
                window.CatBackLoading?.setButtonLoading(submitButton, true, {
                    text: form.dataset.loadingText || 'Đang lưu...'
                });
                if (!window.CatBackLoading) {
                    submitButton.disabled = true;
                    submitButton.textContent = form.dataset.loadingText || 'Đang lưu...';
                }
            }

            try {
                const response = await fetch(form.action || window.location.href, {
                    method: 'POST',
                    body: new FormData(form),
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest',
                        Accept: 'application/json'
                    },
                    credentials: 'same-origin'
                });
                const payload = await readResponse(response);
                syncSavedValues(form, payload);
                showResult('success', {
                    title: payload.title || 'Lưu thành công',
                    message: payload.message || 'Thông tin của bạn đã được cập nhật.',
                    confirmText: 'Đóng'
                });
            } catch (error) {
                renderValidation(form, error.validationErrors);
                const isContactEmailForm = form.dataset.profileForm === 'contact-email';
                showResult('warning', {
                    title: isContactEmailForm
                        ? 'Lưu email thất bại'
                        : 'Lưu tài khoản nhận tiền thất bại',
                    message: error.message,
                    confirmText: 'Kiểm tra lại'
                });
            } finally {
                delete form.dataset.submitting;
                form.removeAttribute('aria-busy');
                if (submitButton) {
                    window.CatBackLoading?.setButtonLoading(submitButton, false);
                    if (!window.CatBackLoading) {
                        submitButton.disabled = false;
                        submitButton.innerHTML = originalButtonHtml;
                    }
                }
            }
        }, { signal });
    });
});

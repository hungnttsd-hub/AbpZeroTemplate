window.CatBackSpa.mount('admin-payouts', ({ signal }) => {
    const money = new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 0 });
    const toast = document.querySelector("[data-admin-payout-toast]");

    function notify(message, error) {
        if (!toast) return;
        toast.textContent = message;
        toast.classList.toggle("is-error", Boolean(error));
        toast.hidden = false;
        window.clearTimeout(notify.timer);
        notify.timer = window.setTimeout(() => { toast.hidden = true; }, 4000);
    }

    async function parse(response) {
        let data = {};
        try { data = await response.json(); } catch (_) { }
        if (response.ok && data.success !== false) return data;
        throw new Error(data.error?.message || data.error || data.message || "Không thể xử lý yêu cầu.");
    }

    function number(selector) {
        const element = document.querySelector(selector);
        return { element, value: Number(element?.textContent.replace(/\D/g, "")) || 0 };
    }

    function updateSummary(item, nextStatus) {
        const amount = Number(item.dataset.amount) || 0;
        const pendingCount = number("[data-summary-pending-count]");
        const pendingAmount = number("[data-summary-pending-amount]");
        if (pendingCount.element) pendingCount.element.textContent = Math.max(0, pendingCount.value - 1);
        if (pendingAmount.element) pendingAmount.element.textContent = `${money.format(Math.max(0, pendingAmount.value - amount))}đ`;
        if (nextStatus === "paid") {
            const count = number("[data-summary-paid-count]");
            const total = number("[data-summary-paid-amount]");
            if (count.element) count.element.textContent = count.value + 1;
            if (total.element) total.element.textContent = `${money.format(total.value + amount)}đ`;
        } else {
            const count = number("[data-summary-rejected-count]");
            if (count.element) count.element.textContent = count.value + 1;
        }
    }

    function addResultLine(container, label, value) {
        const line = document.createElement("p");
        const heading = document.createElement("b");
        heading.textContent = `${label}:`;
        line.append(heading, document.createTextNode(` ${value || "—"}`));
        container.appendChild(line);
    }

    async function copyText(value) {
        if (navigator.clipboard?.writeText) {
            try {
                await navigator.clipboard.writeText(value);
                return;
            } catch (_) { }
        }

        const input = document.createElement("textarea");
        input.value = value;
        input.setAttribute("readonly", "");
        input.style.position = "fixed";
        input.style.opacity = "0";
        document.body.appendChild(input);
        input.select();
        const copied = document.execCommand("copy");
        input.remove();
        if (!copied) throw new Error("Không thể sao chép tự động.");
    }

    document.addEventListener("click", async event => {
        const button = event.target.closest("[data-copy-transfer-content]");
        if (!button) return;

        const content = button.dataset.copyTransferContent?.trim();
        if (!content) return;
        const label = button.querySelector("[data-copy-transfer-label]");
        const originalLabel = label?.textContent || "Sao chép";
        try {
            await copyText(content);
            if (label) label.textContent = "Đã sao chép";
            button.classList.add("is-copied");
            notify(`Đã sao chép: ${content}`);
            window.setTimeout(() => {
                if (label) label.textContent = originalLabel;
                button.classList.remove("is-copied");
            }, 1800);
        } catch (error) {
            notify(error.message, true);
        }
    }, { signal });

    document.addEventListener("submit", async event => {
        const form = event.target.closest("[data-admin-pay], [data-admin-reject]");
        if (!form) return;
        event.preventDefault();
        const isPay = form.hasAttribute("data-admin-pay");
        const confirmed = await window.CatsBackModal.confirm(isPay ? {
            variant: "info",
            title: "Xác nhận đã thanh toán?",
            message: "Hãy chắc chắn bạn đã chuyển đủ tiền và thông tin giao dịch, chứng từ đều chính xác.",
            cancelText: "Kiểm tra lại",
            confirmText: "Xác nhận"
        } : {
            title: "Từ chối yêu cầu rút tiền?",
            message: "Người dùng sẽ nhận được lý do từ chối và số tiền được hoàn lại vào số dư khả dụng.",
            cancelText: "Hủy",
            confirmText: "Từ chối"
        });
        if (!confirmed) return;
        const button = form.querySelector("button[type=submit]");
        const status = form.querySelector("[data-form-status]");
        const original = button.textContent;
        window.CatBackLoading?.setButtonLoading(button, true, { text: "Đang xử lý..." });
        if (!window.CatBackLoading) {
            button.disabled = true;
            button.textContent = "Đang xử lý...";
        }
        status.textContent = "";
        const endLongTask = window.CatBackLoading?.beginLongTask({
            title: isPay ? "Đang xác nhận thanh toán..." : "Đang xử lý từ chối...",
            message: "Vui lòng không tắt cửa sổ này"
        });
        try {
            const payload = await parse(await fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                headers: { "X-Requested-With": "XMLHttpRequest" },
                credentials: "same-origin"
            }));
            const item = form.closest("[data-payout-item]");
            const nextStatus = isPay ? "paid" : "rejected";
            item.dataset.status = nextStatus;
            const badge = item.querySelector("[data-payout-status]");
            badge.className = `admin-payout-status ${nextStatus}`;
            badge.textContent = isPay ? "Đã thanh toán" : "Từ chối";
            const result = item.querySelector("[data-payout-result]");
            result.replaceChildren();
            if (isPay) {
                addResultLine(result, "Mã giao dịch", payload.request.paymentReference);
                const proofLink = document.createElement("a");
                proofLink.href = `/api/app/admin/payouts/${encodeURIComponent(payload.request.id)}/proof`;
                proofLink.target = "_blank";
                proofLink.rel = "noopener";
                proofLink.textContent = "Xem chứng từ";
                result.appendChild(proofLink);
            } else {
                addResultLine(result, "Lý do", payload.request.rejectionReason);
            }
            item.querySelector("[data-payout-actions]")?.remove();
            item.querySelector(".admin-payout-transfer-content")?.remove();
            item.querySelector(".admin-payout-warning")?.remove();
            updateSummary(item, nextStatus);
            notify(payload.message);
        } catch (error) {
            window.CatBackLoading?.setButtonLoading(button, false);
            if (!window.CatBackLoading) {
                button.disabled = false;
                button.textContent = original;
            }
            status.textContent = error.message;
            notify(error.message, true);
        } finally {
            endLongTask?.();
        }
    }, { signal });
});

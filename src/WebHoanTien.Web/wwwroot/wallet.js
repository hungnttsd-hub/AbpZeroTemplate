window.CatBackSpa.mount('wallet', ({ signal }) => {
    const money = new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 0 });
    const formatMoney = value => `${money.format(Math.max(0, Number(value) || 0))}đ`;

    function showToast(message, isError) {
        const toast = document.querySelector("[data-wallet-toast]");
        if (!toast) return;
        toast.textContent = message;
        toast.classList.toggle("is-error", Boolean(isError));
        toast.hidden = false;
        window.clearTimeout(showToast.timer);
        showToast.timer = window.setTimeout(() => { toast.hidden = true; }, 3600);
    }

    async function readResponse(response) {
        let payload = {};
        try { payload = await response.json(); } catch (_) { }
        if (response.ok && payload.success !== false) return payload;
        const error = payload.error?.message || payload.error || payload.message || "Không thể xử lý yêu cầu. Vui lòng thử lại.";
        throw new Error(typeof error === "string" ? error : "Không thể xử lý yêu cầu.");
    }

    function updateBalance(value) {
        document.querySelectorAll("[data-wallet-available], [data-wallet-available-inline]")
            .forEach(element => { element.textContent = formatMoney(value); });
    }

    document.addEventListener("submit", async event => {
        const cancelForm = event.target.closest("[data-wallet-cancel]");
        if (!cancelForm) return;
        event.preventDefault();
        const button = cancelForm.querySelector("button[type=submit]");
        const confirmed = await window.CatsBackModal.confirm({
            title: "Hủy yêu cầu rút tiền?",
            message: "Yêu cầu đang chờ sẽ được hủy và số tiền được hoàn lại vào số dư khả dụng.",
            cancelText: "Giữ yêu cầu",
            confirmText: "Hủy yêu cầu"
        });
        if (!confirmed) return;
        window.CatBackLoading?.setButtonLoading(button, true, { text: "Đang hủy..." });
        if (!window.CatBackLoading) button.disabled = true;
        try {
            const payload = await readResponse(await fetch(cancelForm.action, {
                method: "POST",
                body: new FormData(cancelForm),
                headers: { "X-Requested-With": "XMLHttpRequest" },
                credentials: "same-origin"
            }));
            updateBalance(payload.availableBalance);
            cancelForm.closest("[data-pending-withdrawal]")?.remove();
            const request = document.querySelector(`[data-wallet-request="${payload.request.id}"]`);
            if (request) {
                const status = request.querySelector("[data-wallet-status]");
                status.textContent = "Đã hủy";
                status.className = "wallet-status cancelled";
            }
            showToast(payload.message || "Đã hủy yêu cầu rút tiền.");
        } catch (error) {
            showToast(error.message, true);
        } finally {
            window.CatBackLoading?.setButtonLoading(button, false);
            if (!window.CatBackLoading) button.disabled = false;
        }
    }, { signal });

    const withdrawForm = document.querySelector("[data-withdraw-form]");
    if (!withdrawForm) return;

    const input = withdrawForm.querySelector("#withdraw-amount");
    const amountValue = withdrawForm.querySelector("[data-withdraw-value]");
    const submit = withdrawForm.querySelector(".wallet-submit-button");
    const errorBox = withdrawForm.querySelector("[data-withdraw-error]");
    const statusBox = withdrawForm.querySelector("[data-withdraw-status]");
    const summary = withdrawForm.querySelector("[data-withdraw-summary]");
    const net = withdrawForm.querySelector("[data-withdraw-net]");
    const available = Number(withdrawForm.dataset.available) || 0;
    const fee = Number(withdrawForm.dataset.fee) || 0;

    function currentAmount() {
        return Number((input.value || "").replace(/[^0-9]/g, "")) || 0;
    }

    function setAmount(value) {
        const amount = Math.max(0, Math.floor(Number(value) || 0));
        input.value = amount > 0 ? money.format(amount) : "";
        amountValue.value = amount;
        renderAmount();
    }

    function renderAmount() {
        const amount = currentAmount();
        amountValue.value = amount;
        summary.textContent = formatMoney(amount);
        net.textContent = formatMoney(Math.max(0, amount - fee));
        errorBox.hidden = true;
        withdrawForm.querySelectorAll("[data-withdraw-amount], [data-withdraw-all]").forEach(button => {
            const candidate = button.hasAttribute("data-withdraw-all") ? available : Number(button.dataset.withdrawAmount);
            button.classList.toggle("is-active", candidate === amount);
        });
    }

    withdrawForm.querySelectorAll("[data-withdraw-amount]").forEach(button => {
        button.addEventListener("click", () => { setAmount(Math.min(available, Number(button.dataset.withdrawAmount))); input.focus(); }, { signal });
    });
    withdrawForm.querySelector("[data-withdraw-all]")?.addEventListener("click", () => { setAmount(available); input.focus(); }, { signal });
    input?.addEventListener("input", () => setAmount(currentAmount()), { signal });

    withdrawForm.addEventListener("submit", async event => {
        event.preventDefault();
        const amount = currentAmount();
        if (amount < 10000) {
            errorBox.textContent = "Số tiền rút tối thiểu là 10.000đ.";
            errorBox.hidden = false;
            input.focus();
            return;
        }
        if (amount > available) {
            errorBox.textContent = "Số tiền yêu cầu vượt quá số dư khả dụng.";
            errorBox.hidden = false;
            input.focus();
            return;
        }

        window.CatBackLoading?.setButtonLoading(submit, true, { text: "Đang gửi yêu cầu..." });
        if (!window.CatBackLoading) {
            submit.disabled = true;
            submit.textContent = "Đang gửi yêu cầu...";
        }
        statusBox.textContent = "";
        const endLongTask = window.CatBackLoading?.beginLongTask({
            title: "Đang gửi yêu cầu rút tiền...",
            message: "CatBack đang kiểm tra và ghi nhận yêu cầu của bạn."
        });
        try {
            const payload = await readResponse(await fetch(withdrawForm.action, {
                method: "POST",
                body: new FormData(withdrawForm),
                headers: { "X-Requested-With": "XMLHttpRequest" },
                credentials: "same-origin"
            }));
            updateBalance(payload.availableBalance);
            withdrawForm.dataset.available = payload.availableBalance;
            input.disabled = true;
            withdrawForm.querySelectorAll(".wallet-quick-amounts button").forEach(button => { button.disabled = true; });
            window.CatBackLoading?.setButtonLoading(submit, false);
            submit.textContent = "Yêu cầu đang chờ xử lý";
            submit.disabled = true;
            statusBox.textContent = payload.message;
            const history = document.querySelector(".wallet-withdrawal-history");
            if (history) {
                const empty = history.querySelector(".wallet-history-empty");
                empty?.remove();
                const item = document.createElement("article");
                item.dataset.walletRequest = payload.request.id;
                item.innerHTML = `<div><strong>${payload.request.requestCode}</strong><small>Vừa gửi</small></div><div><b>${formatMoney(payload.request.amount)}</b><span class="wallet-status pending" data-wallet-status>Đang xử lý</span></div>`;
                history.appendChild(item);
            }
            showToast(payload.message || "Đã gửi yêu cầu rút tiền.");
        } catch (error) {
            window.CatBackLoading?.setButtonLoading(submit, false);
            if (!window.CatBackLoading) {
                submit.disabled = false;
                submit.textContent = "Yêu cầu rút tiền";
            }
            statusBox.textContent = "";
            errorBox.textContent = error.message;
            errorBox.hidden = false;
            showToast(error.message, true);
        } finally {
            endLongTask?.();
        }
    }, { signal });

    renderAmount();
});

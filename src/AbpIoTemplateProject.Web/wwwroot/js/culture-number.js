(() => {
    "use strict";

    const selector = "input.js-culture-number[data-culture-number-target]";

    function getFormatter(input, useGrouping) {
        const decimals = Number.parseInt(input.dataset.cultureNumberDecimals || "0", 10);
        return new Intl.NumberFormat(input.dataset.cultureNumber || undefined, {
            useGrouping,
            minimumFractionDigits: decimals,
            maximumFractionDigits: decimals
        });
    }

    function getSeparators(formatter) {
        const parts = formatter.formatToParts(12345.6);
        return {
            group: parts.find(part => part.type === "group")?.value || ",",
            decimal: parts.find(part => part.type === "decimal")?.value || "."
        };
    }

    function parseLocalized(input) {
        const text = input.value.trim();
        if (!text) return null;

        const formatter = getFormatter(input, true);
        const separators = getSeparators(formatter);
        let normalized = text.replace(/[\s\u00A0\u202F]/g, "");
        if (separators.group) normalized = normalized.split(separators.group).join("");
        if (separators.decimal && separators.decimal !== ".") {
            normalized = normalized.split(separators.decimal).join(".");
        }
        normalized = normalized.replace(/[^0-9+\-.]/g, "");
        const number = Number(normalized);
        return Number.isFinite(number) ? number : null;
    }

    function sync(input, formatDisplay) {
        const target = document.getElementById(input.dataset.cultureNumberTarget);
        if (!target) return;

        const number = parseLocalized(input);
        if (number === null) {
            target.value = "";
            return;
        }

        target.value = getFormatter(input, false).format(number);
        if (formatDisplay) input.value = getFormatter(input, true).format(number);
    }

    function initialize(input) {
        if (input.dataset.cultureNumberInitialized === "true") return;
        input.dataset.cultureNumberInitialized = "true";
        input.addEventListener("input", () => sync(input, false));
        input.addEventListener("change", () => sync(input, true));
        input.addEventListener("blur", () => sync(input, true));
        sync(input, true);
    }

    function initializeAll(root) {
        root.querySelectorAll(selector).forEach(initialize);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", () => initializeAll(document));
    } else {
        initializeAll(document);
    }
})();

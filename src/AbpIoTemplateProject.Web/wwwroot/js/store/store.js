(function () {
    "use strict";

    var backToTopButton = document.querySelector("[data-store-back-to-top]");
    var searchForms = document.querySelectorAll("[data-store-search]");
    var newsletterForm = document.querySelector("[data-store-newsletter]");

    if (backToTopButton) {
        var updateBackToTopVisibility = function () {
            backToTopButton.classList.toggle("is-visible", window.scrollY > 480);
        };

        updateBackToTopVisibility();
        window.addEventListener("scroll", updateBackToTopVisibility, { passive: true });
        backToTopButton.addEventListener("click", function () {
            window.scrollTo({ top: 0, behavior: "smooth" });
        });
    }

    searchForms.forEach(function (searchForm) {
        var searchInput = searchForm.querySelector('input[type="search"]');
        var suggestions = searchForm.querySelector(".store-search-suggestions");
        var searchTimer;
        var searchController;

        var closeSuggestions = function () {
            if (!suggestions || !searchInput) {
                return;
            }

            suggestions.hidden = true;
            searchInput.setAttribute("aria-expanded", "false");
        };

        if (!searchInput || !suggestions) {
            return;
        }

        searchInput.addEventListener("input", function () {
            window.clearTimeout(searchTimer);
            searchTimer = window.setTimeout(function () {
                var query = searchInput.value.trim();
                if (query.length < 2) {
                    closeSuggestions();
                    return;
                }

                if (searchController) {
                    searchController.abort();
                }

                searchController = new AbortController();
                fetch("/products?handler=Suggestions&query=" + encodeURIComponent(query), {
                    signal: searchController.signal,
                    headers: { "Accept": "application/json" }
                })
                    .then(function (response) {
                        if (!response.ok) {
                            throw new Error("Search request failed");
                        }
                        return response.json();
                    })
                    .then(function (items) {
                        suggestions.replaceChildren();
                        if (!items.length) {
                            var empty = document.createElement("p");
                            empty.textContent = "Không tìm thấy sản phẩm phù hợp.";
                            suggestions.appendChild(empty);
                        } else {
                            items.forEach(function (item) {
                                var link = document.createElement("a");
                                link.href = "/products/" + encodeURIComponent(item.slug);
                                link.setAttribute("role", "option");

                                var image = document.createElement("img");
                                image.src = item.imageUrl || "/images/store/product-placeholder.svg";
                                image.alt = "";

                                var copy = document.createElement("span");
                                var name = document.createElement("strong");
                                name.textContent = item.name;
                                var meta = document.createElement("small");
                                var price = item.salePrice == null ? item.listPrice : item.salePrice;
                                meta.textContent = item.sku + (price == null ? "" : " · " + new Intl.NumberFormat("vi-VN").format(price) + " ₫");
                                copy.append(name, meta);
                                link.append(image, copy);
                                suggestions.appendChild(link);
                            });
                        }
                        suggestions.hidden = false;
                        searchInput.setAttribute("aria-expanded", "true");
                    })
                    .catch(function (error) {
                        if (error.name !== "AbortError") {
                            closeSuggestions();
                        }
                    });
            }, 250);
        });

        document.addEventListener("click", function (event) {
            if (!searchForm.contains(event.target)) {
                closeSuggestions();
            }
        });

        searchInput.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                closeSuggestions();
            }
        });
    });

    if (newsletterForm) {
        newsletterForm.addEventListener("submit", function (event) {
            event.preventDefault();

            var emailInput = newsletterForm.querySelector('input[type="email"]');
            var status = newsletterForm.querySelector("[data-store-newsletter-status]");

            if (!emailInput || !status) {
                return;
            }

            if (!emailInput.checkValidity()) {
                emailInput.setAttribute("aria-invalid", "true");
                status.textContent = emailInput.validationMessage;
                emailInput.focus();
                return;
            }

            emailInput.removeAttribute("aria-invalid");
            status.textContent = newsletterForm.dataset.successMessage || "";
            emailInput.value = "";
        });
    }
})();

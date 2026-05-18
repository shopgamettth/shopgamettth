$(document).ready(function () {

    console.log("JS loaded");

    // =========================
    // 📦 GET CONTAINER CHUNG (FIX PRODUCT + DETAILS)
    // =========================
    function getContainer(el) {
        return $(el).closest(".product-card, .container");
    }

    function getInput(el) {
        return getContainer(el).find(".quantity-input");
    }

    // =========================
    // ➕ ➖ BUTTON
    // =========================
    $(document).on("click", ".btn-minus", function () {

        let input = getInput(this);

        let value = parseInt(input.val()) || 1;

        if (value > 1) {
            input.val(value - 1);
        }
    });

    $(document).on("click", ".btn-plus", function () {

        let input = getInput(this);

        let value = parseInt(input.val()) || 1;

        if (value < 99) {
            input.val(value + 1);
        }
    });

    // =========================
    // 🧼 CHẶN INPUT BẨN
    // =========================

    // ❌ chặn paste
    $(document).on("paste", ".quantity-input", function (e) {
        e.preventDefault();
    });

    // ❌ chỉ cho số
    $(document).on("keypress", ".quantity-input", function (e) {
        if (e.which < 48 || e.which > 57) {
            e.preventDefault();
        }
    });

    // 🔥 sanitize khi nhập
    $(document).on("input", ".quantity-input", function () {

        let value = $(this).val();

        value = value.replace(/[^0-9]/g, "");

        if (value === "") {
            $(this).val("");
            return;
        }

        value = parseInt(value);

        if (value < 1) value = 1;
        if (value > 99) value = 99;

        $(this).val(value);
    });

    // 🔥 blur fix
    $(document).on("blur", ".quantity-input", function () {

        let value = parseInt($(this).val());

        if (isNaN(value) || value < 1) {
            $(this).val(1);
        }

        if (value > 99) {
            $(this).val(99);
        }
    });

    // =========================
    // 🛒 ADD TO CART
    // =========================
    $(document).on("click", ".btn-add-cart", function () {
        let container = getContainer(this);
        let productId = $(this).data("productid");
        let quantity = parseInt(container.find(".quantity-input").val()) || 1;

        // =========================
        // 🚀 AJAX ADD
        // =========================
        $.ajax({
            url: "/Cart/AddToCart",
            type: "POST",
            data: {
                productId: productId,
                quantity: quantity
            },
            success: function (res) {
                if (res.success) {
                    $(".cart-count").text(res.cartCount);
                    showToast(res.message || "Đã thêm vào giỏ hàng!", "success");
                } else {
                    showToast(res.message || "Không thể thêm vào giỏ hàng!", "error");
                }
            },
            error: function (xhr) {
                if (xhr.status === 401) {
                    showToast("Vui lòng đăng nhập để thêm vào giỏ hàng!", "warning");
                } else {
                    showToast("Thêm vào giỏ hàng thất bại!", "error");
                }
            }
        });
    });

});

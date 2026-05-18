// =========================
// 🔥 CLICK EVENTS
// =========================
document.addEventListener("click", function (e) {

    // ❌ BỎ CHỌN TẤT CẢ
    if (e.target.id === "uncheck-all") {

        document.querySelectorAll(".item-check").forEach(cb => {
            cb.checked = false;
        });

        let checkAll = document.getElementById("check-all");
        if (checkAll) checkAll.checked = false;

        updateTotal();
        return;
    }

    let row = e.target.closest("tr");
    if (!row) return;

    let productId = row.dataset.id;

    // ➕ PLUS
    if (e.target.classList.contains("btn-plus")) {
        updateQty(productId, true, row);
    }

    // ➖ MINUS
    if (e.target.classList.contains("btn-minus")) {
        updateQty(productId, false, row);
    }

    // 🗑 DELETE
    if (e.target.classList.contains("btn-delete")) {
        if (!confirm("Bạn có chắc chắn muốn xoá không?")) return;

        fetch("/Cart/RemoveItem", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: `productId=${productId}`
        })
            .then(r => r.json())
            .then(res => {
                if (res.success) {
                    row.remove();

                    let badge = document.querySelector(".cart-count");
                    if (badge) badge.innerText = res.cartCount;

                    updateTotal();

                    // ✅ Hiển thị toast xoá thành công
                    showToast("Đã xoá sản phẩm khỏi giỏ hàng!", "success");
                } else {
                    showToast("Xoá thất bại, vui lòng thử lại!", "error");
                }
            })
            .catch(() => {
                showToast("Có lỗi xảy ra, vui lòng thử lại!", "error");
            });
    }
});


// =========================
// 🚀 UPDATE QTY
// =========================
function updateQty(productId, isPlus, row) {

    fetch("/Cart/UpdateQty", {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: `productId=${productId}&isPlus=${isPlus}`
    })
        .then(r => r.json())
        .then(res => {

            // quantity
            row.querySelector(".quantity-input").value = res.quantity;

            // subtotal
            row.querySelector(".subtotal").innerText =
                Number(res.subTotal).toLocaleString();

            // badge
            let badge = document.querySelector(".cart-count");
            if (badge) badge.innerText = res.cartCount;

            // 🔥 total realtime
            updateTotal();
        });
}


// =========================
// 💰 TOTAL THEO CHECKBOX
// =========================
function updateTotal() {

    let total = 0;

    document.querySelectorAll("#cart-body tr").forEach(row => {

        let checkbox = row.querySelector(".item-check");

        if (checkbox && checkbox.checked) {

            let subtotal = row.querySelector(".subtotal")
                .innerText.replace(/[.,]/g, ""); // 🔥 FIX

            total += parseFloat(subtotal) || 0;
        }
    });

    let totalEl = document.getElementById("cart-total");
    if (totalEl) {
        totalEl.innerText = total.toLocaleString();
    }
}


// =========================
// ✅ CHECKBOX
// =========================
document.addEventListener("change", function (e) {

    // CHECK ALL
    if (e.target.id === "check-all") {

        let checked = e.target.checked;

        document.querySelectorAll(".item-check").forEach(cb => {
            cb.checked = checked;
        });

        updateTotal();
    }

    // CHECK ITEM
    if (e.target.classList.contains("item-check")) {

        let all = document.querySelectorAll(".item-check").length;
        let checked = document.querySelectorAll(".item-check:checked").length;

        let checkAll = document.getElementById("check-all");
        if (checkAll) {
            checkAll.checked = (all === checked);
        }

        updateTotal();
    }
});


// =========================
// 🔍 SEARCH
// =========================
let search = document.getElementById("search-cart");

if (search) {
    search.addEventListener("input", function () {

        let keyword = this.value.toLowerCase();

        document.querySelectorAll("#cart-body tr").forEach(row => {

            let name = row.querySelector(".product-name")
                .innerText.toLowerCase();

            row.style.display = name.includes(keyword) ? "" : "none";
        });
    });
}


// =========================
// 🔥 INPUT FIX (1 → 99)
// =========================

// ❌ paste
document.addEventListener("paste", function (e) {
    if (e.target.classList.contains("quantity-input")) {
        e.preventDefault();
    }
});

// ❌ ký tự
document.addEventListener("keypress", function (e) {
    if (e.target.classList.contains("quantity-input")) {
        if (e.which < 48 || e.which > 57) {
            e.preventDefault();
        }
    }
});

// sanitize
document.addEventListener("input", function (e) {

    if (!e.target.classList.contains("quantity-input")) return;

    let value = e.target.value.replace(/[^0-9]/g, "");

    if (value === "") {
        e.target.value = "";
        return;
    }

    value = parseInt(value);

    if (value < 1) value = 1;
    if (value > 99) value = 99;

    e.target.value = value;
});

// blur fix
document.addEventListener("blur", function (e) {

    if (!e.target.classList.contains("quantity-input")) return;

    let value = parseInt(e.target.value);

    if (isNaN(value) || value < 1) e.target.value = 1;
    if (value > 99) e.target.value = 99;

}, true);

// =========================
// 🔥 UPDATE KHI NHẬP TAY
// =========================
document.addEventListener("blur", function (e) {

    if (!e.target.classList.contains("quantity-input")) return;

    let input = e.target;
    let row = input.closest("tr");

    if (!row) return;

    let productId = row.dataset.id;

    let value = parseInt(input.value);

    // fix input
    if (isNaN(value) || value < 1) value = 1;
    if (value > 99) value = 99;

    input.value = value;

    // 🚀 gọi API mới (set trực tiếp)
    fetch("/Cart/SetQty", {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: `productId=${productId}&quantity=${value}`
    })
        .then(r => r.json())
        .then(res => {

            // update subtotal
            row.querySelector(".subtotal").innerText =
                Number(res.subTotal).toLocaleString();

            // update badge
            let badge = document.querySelector(".cart-count");
            if (badge) badge.innerText = res.cartCount;

            updateTotal();
        });

}, true);

// =========================
// 🛒 CHECKOUT FORM
// =========================
function hasSelectedCheckoutItems() {
    return document.querySelectorAll(".item-check:checked").length > 0;
}

window.addEventListener("DOMContentLoaded", function () {

    let form = document.getElementById("cart-checkout-form");

    if (!form) return;

    form.addEventListener("submit", function (e) {
        if (!hasSelectedCheckoutItems()) {
            e.preventDefault();
            alert("Vui lòng chọn sản phẩm!");
        }
    });
});

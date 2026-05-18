//$(document).ready(function () {
//    const cartBadges = $(".cart-count");

//    if (!cartBadges.length) {
//        return;
//    }

//    $.ajax({
//        url: "/Cart/GetCartCount",
//        type: "GET",
//        success: function (res) {
//            cartBadges.text(res.count ?? 0);
//        },
//        error: function () {
//            cartBadges.text("0");
//        }
//    });
//});



async function refreshCartBadge() {
    const cartBadges = document.querySelectorAll(".cart-count");
    console.log(cartBadges);
    if (!cartBadges.length) return;

    try {
        const res = await axios.get("/Cart/GetCartCount");
        console.log(res.data.count);
        cartBadges.forEach(el => el.textContent = res.data.count);
    } catch {
        cartBadges.forEach(el => el.textContent = "0");
    }
}

$(document).ready(refreshCartBadge); // jquery

//document.addEventListener("DOMContentLoaded", refreshCartBadge);
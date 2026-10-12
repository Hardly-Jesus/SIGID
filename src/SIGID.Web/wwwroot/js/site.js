// Comportamientos sencillos de la interfaz de SIGID.

// Menú lateral en pantallas pequeñas
document.querySelectorAll("[data-sidebar-open]").forEach(function (button) {
    button.addEventListener("click", function () { document.body.classList.add("sidebar-open"); });
});
document.querySelectorAll("[data-sidebar-close]").forEach(function (el) {
    el.addEventListener("click", function () { document.body.classList.remove("sidebar-open"); });
});

// Cerrar avisos
document.querySelectorAll("[data-dismiss]").forEach(function (button) {
    button.addEventListener("click", function () { button.closest("[data-dismissible]").remove(); });
});

// Entradas y salidas: muestra la existencia actual del producto elegido
// y cómo quedará después del movimiento (solo informativo; el servidor valida).
document.querySelectorAll("[data-stock-form]").forEach(function (form) {
    var select = form.querySelector("[data-product-select]");
    var quantity = form.querySelector("[data-quantity]");
    var preview = form.querySelector("[data-stock-preview]");
    if (!select || !preview) return;

    var isExit = form.getAttribute("data-stock-form") === "exit";
    var current = preview.querySelector("[data-current]");
    var after = preview.querySelector("[data-after]");
    var afterBox = preview.querySelector("[data-after-box]");

    function update() {
        var option = select.options[select.selectedIndex];
        var stock = option ? option.getAttribute("data-stock") : null;
        if (stock === null) { preview.hidden = true; return; }

        var unit = option.getAttribute("data-unit") || "";
        var qty = parseInt(quantity && quantity.value, 10);
        var stockNumber = parseInt(stock, 10);

        preview.hidden = false;
        current.textContent = stockNumber + " " + unit;

        if (qty > 0) {
            var result = isExit ? stockNumber - qty : stockNumber + qty;
            afterBox.hidden = false;
            after.textContent = (result < 0 ? "Insuficiente" : result + " " + unit);
            after.classList.toggle("is-negative", result < 0);
        } else {
            afterBox.hidden = true;
        }
    }

    select.addEventListener("change", update);
    if (quantity) quantity.addEventListener("input", update);
    update();
});

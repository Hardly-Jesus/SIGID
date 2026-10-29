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

// Formularios marcados con data-submit-once: evita que un doble clic envíe dos veces.
// No se deshabilitan los botones para que el servidor reciba cuál se pulsó.
// (El servidor también evita ventas duplicadas mediante el identificador del formulario.)
document.querySelectorAll("form[data-submit-once]").forEach(function (form) {
    form.addEventListener("submit", function (event) {
        if (form.dataset.submitting === "true") {
            event.preventDefault();
            return;
        }
        form.dataset.submitting = "true";
    });
});

// Al volver con el botón «Atrás» el formulario debe poder enviarse otra vez.
window.addEventListener("pageshow", function () {
    document.querySelectorAll("form[data-submit-once]").forEach(function (form) {
        delete form.dataset.submitting;
    });
});

// Registrar venta: al cambiar la cantidad de un producto ya agregado, avisa en el momento
// si supera las existencias. Es solo una ayuda: el servidor vuelve a validar todo.
document.querySelectorAll("[data-sale-qty]").forEach(function (input) {
    var message = input.parentElement.querySelector("[data-valmsg-for]");
    var stock = parseInt(input.getAttribute("data-stock"), 10);
    var unit = input.getAttribute("data-unit") || "";
    if (!message || isNaN(stock)) return;

    input.addEventListener("input", function () {
        var value = input.value.trim();
        var qty = Number(value);
        var text = "";

        if (value === "" || !Number.isInteger(qty) || qty <= 0) {
            text = "Escribe una cantidad mayor que cero.";
        } else if (qty > stock) {
            text = stock > 0 ? "Solo hay " + stock + " " + unit + " disponibles." : "No hay existencias disponibles.";
        }

        message.textContent = text;
        message.classList.toggle("field-validation-error", text !== "");
        message.classList.toggle("field-validation-valid", text === "");
        input.classList.toggle("input-validation-error", text !== "");
    });
});

// Admin/EditJob.aspx
// Shows the change while the owner types the cash for the difference.
// Display only: the server checks the cash and computes the change again on save.
(function () {
    function peso(n) {
        return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    // Product add-on steppers. These only keep each row's own line total honest; the
    // price summary beside the form is recomputed by the server on save, the same way a
    // corrected weight is. Runs in both the collect and the refund case.
    Array.prototype.forEach.call(document.querySelectorAll('[data-product]'), function (row) {
        var box = row.querySelector('[data-qty]');
        var price = parseFloat(row.getAttribute('data-price'));

        function paint() {
            var qty = parseInt(box.value, 10);
            if (isNaN(qty) || qty < 0) { qty = 0; }
            row.className = 'product-row ' + (qty === 0 ? 'is-zero' : 'is-on');
            row.querySelector('.line-total').textContent = peso(qty * price);
            row.querySelector('[data-qty-step="-1"]').disabled = qty === 0;
            return qty;
        }

        box.addEventListener('input', paint);
        Array.prototype.forEach.call(row.querySelectorAll('[data-qty-step]'), function (btn) {
            btn.addEventListener('click', function () {
                var next = paint() + parseInt(btn.getAttribute('data-qty-step'), 10);
                box.value = next < 0 ? 0 : next;
                paint();
            });
        });
        paint();
    });

    var cashFields = document.getElementById('adjCashFields');
    if (!cashFields) return;   // refund case: there is no cash to count

    var due = parseFloat(cashFields.getAttribute('data-due'));   // written by the page
    var cash = document.getElementById('txtAdjCashReceived');
    var rbCash = document.getElementById('rbAdjCash');
    var rbEwallet = document.getElementById('rbAdjEwallet');

    function update() {
        cashFields.style.display = rbCash.checked ? '' : 'none';
        var change = (parseFloat(cash.value.replace(/,/g, '')) || 0) - due;
        document.getElementById('adjChangeBox').className = 'change-box' + (change >= 0 ? '' : ' short');
        document.getElementById('adjChange').textContent = change >= 0 ? peso(change) : 'Short ' + peso(-change);
    }

    cash.addEventListener('input', update);
    rbCash.addEventListener('change', update);
    rbEwallet.addEventListener('change', update);
    update();
})();

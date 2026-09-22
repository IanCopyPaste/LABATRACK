// Admin/EditJob.aspx
// Shows the change while the owner types the cash for the difference.
// Display only: the server checks the cash and computes the change again on save.
(function () {
    var cashFields = document.getElementById('adjCashFields');
    if (!cashFields) return;   // refund case: there is no cash to count

    var due = parseFloat(cashFields.getAttribute('data-due'));   // written by the page
    var cash = document.getElementById('txtAdjCashReceived');
    var rbCash = document.getElementById('rbAdjCash');
    var rbEwallet = document.getElementById('rbAdjEwallet');

    function peso(n) {
        return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

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

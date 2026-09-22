// Pages/Shared/NewJob.aspx
// Shows the change while the cashier types. Display only: the server checks the
// cash and computes the change again on save, and that is the value that is stored.
(function () {
    var paymentCard = document.getElementById('paymentCard');
    var total = parseFloat(paymentCard.getAttribute('data-total'));   // written by the page
    var cash = document.getElementById('txtCashReceived');
    var rbCash = document.getElementById('rbCash');
    var rbEwallet = document.getElementById('rbEwallet');
    var createBtn = document.getElementById('btnCreate');

    function peso(n) {
        return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function update() {
        var isCash = rbCash.checked;
        document.getElementById('cashFields').style.display = isCash ? '' : 'none';
        document.getElementById('cashSummary').style.display = isCash ? '' : 'none';
        document.getElementById('ewalletNote').style.display = isCash ? 'none' : '';

        var received = parseFloat(cash.value.replace(/,/g, '')) || 0;
        var change = received - total;
        var enough = !isCash || change >= 0;

        document.getElementById('changeBox').className = 'change-box' + (enough ? '' : ' short');
        document.getElementById('changeAmount').textContent = enough ? peso(change) : 'Short ' + peso(-change);
        document.getElementById('sumReceived').textContent = peso(received);
        document.getElementById('sumChange').textContent = enough ? peso(change) : '—';
        document.getElementById('sumMethod').textContent = 'Paid in full · ' + (isCash ? 'Cash' : 'E-wallet');
        createBtn.disabled = !enough;
    }

    cash.addEventListener('input', update);
    rbCash.addEventListener('change', update);
    rbEwallet.addEventListener('change', update);
    document.querySelectorAll('[data-cash]').forEach(function (b) {
        b.addEventListener('click', function () {
            cash.value = parseFloat(b.getAttribute('data-cash')).toFixed(2);
            update();
        });
    });
    update();
})();

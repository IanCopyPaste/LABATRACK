// Pages/Shared/NewJob.aspx
// Keeps the order summary and the change in step with what the cashier ticks and types.
// Display only: on save the server recomputes the total from the stored prices and checks
// the cash again, and those are the values that are stored.
(function () {
    var paymentCard = document.getElementById('paymentCard');
    var laundry = parseFloat(paymentCard.getAttribute('data-laundry'));   // written by the page
    var cash = document.getElementById('txtCashReceived');
    var rbCash = document.getElementById('rbCash');
    var rbEwallet = document.getElementById('rbEwallet');
    var createBtn = document.getElementById('btnCreate');
    var exactBtn = document.getElementById('btnExact');
    var sumLines = document.getElementById('sumLines');
    var services = document.querySelectorAll('[data-service]');
    var products = document.querySelectorAll('[data-product]');

    function peso(n) {
        if (!isFinite(n)) { return '—'; }
        return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function qtyBox(row) {
        return row.querySelector('[data-qty]');
    }

    // A blank or junk quantity counts as none rather than breaking the total.
    function qtyOf(row) {
        var n = parseInt(qtyBox(row).value, 10);
        if (isNaN(n) || n < 0) { n = 0; }
        return n;
    }

    // Services are charged once; products are quantity x unit price. Returns the
    // add-on lines in the order they appear on the form so the summary reads top to bottom.
    function addOnLines() {
        var lines = [];
        Array.prototype.forEach.call(services, function (box) {
            if (box.checked) {
                lines.push({ label: box.getAttribute('data-name'), amount: parseFloat(box.getAttribute('data-price')) });
            }
        });
        Array.prototype.forEach.call(products, function (row) {
            var qty = qtyOf(row);
            if (qty > 0) {
                var price = parseFloat(row.getAttribute('data-price'));
                lines.push({ label: row.getAttribute('data-name') + ' × ' + qty, amount: qty * price });
            }
        });
        return lines;
    }

    // Each product row shows its own line total. A row in play is highlighted and one at
    // zero is dimmed, so the cashier can read off what is on the order at a glance while
    // still seeing everything the shop sells.
    function paintProducts() {
        Array.prototype.forEach.call(products, function (row) {
            var qty = qtyOf(row);
            var price = parseFloat(row.getAttribute('data-price'));
            row.className = 'product-row ' + (qty === 0 ? 'is-zero' : 'is-on');
            row.querySelector('.line-total').textContent = peso(qty * price);
            row.querySelector('[data-qty-step="-1"]').disabled = qty === 0;
        });
    }

    // The server renders these rows too, so the summary is already right before this runs.
    function paintSummary(lines) {
        Array.prototype.forEach.call(sumLines.querySelectorAll('.addon-line'), function (el) {
            sumLines.removeChild(el);
        });
        lines.forEach(function (line) {
            var dt = document.createElement('dt');
            dt.className = 'addon-line';
            dt.textContent = line.label;
            var dd = document.createElement('dd');
            dd.className = 'addon-line';
            dd.textContent = peso(line.amount);
            sumLines.appendChild(dt);
            sumLines.appendChild(dd);
        });
    }

    function update() {
        paintProducts();

        var lines = addOnLines();
        paintSummary(lines);

        var total = lines.reduce(function (sum, line) { return sum + line.amount; }, laundry);
        document.getElementById('sumTotal').textContent = peso(total);
        document.getElementById('ewalletTotal').textContent = peso(total);
        if (isFinite(total)) { exactBtn.setAttribute('data-cash', total.toFixed(2)); }

        var isCash = rbCash.checked;
        document.getElementById('cashFields').style.display = isCash ? '' : 'none';
        document.getElementById('cashSummary').style.display = isCash ? '' : 'none';
        document.getElementById('ewalletNote').style.display = isCash ? 'none' : '';

        // If the total cannot be worked out here, say nothing rather than print NaN, and
        // leave the button alone: the server checks the cash on save either way.
        if (!isFinite(total)) {
            paintChange('idle', 'Change', '—');
            createBtn.disabled = false;
            return;
        }

        var typed = cash.value.replace(/[,₱\s]/g, '');
        var received = parseFloat(typed);
        // Worked in centavos so 500.00 - 289.99 never comes out as 210.00999...
        var change = (Math.round(received * 100) - Math.round(total * 100)) / 100;

        var state;
        if (!isCash) { state = 'ok'; }
        else if (typed === '' || isNaN(received)) { state = 'idle'; }
        else if (change < 0) { state = 'short'; }
        else if (change === 0) { state = 'exact'; }
        else { state = 'ok'; }

        if (state === 'idle') { paintChange('idle', 'Change', 'Enter cash received'); }
        else if (state === 'short') { paintChange('short', 'Short by', peso(-change)); }
        else if (state === 'exact') { paintChange('exact', 'Exact amount', 'No change'); }
        else if (isCash) { paintChange('', 'Change', peso(change)); }

        var receivedOk = isCash && state !== 'idle';
        document.getElementById('sumReceived').textContent = receivedOk ? peso(received) : '—';
        document.getElementById('sumChange').textContent = (state === 'ok' || state === 'exact') && isCash ? peso(change) : '—';
        document.getElementById('sumMethod').textContent = 'Paid in full · ' + (isCash ? 'Cash' : 'E-wallet');
        createBtn.disabled = isCash && (state === 'idle' || state === 'short');
    }

    function paintChange(state, label, amount) {
        document.getElementById('changeBox').className = 'change-box' + (state ? ' ' + state : '');
        document.getElementById('changeLabel').textContent = label;
        document.getElementById('changeAmount').textContent = amount;
    }

    cash.addEventListener('input', update);
    rbCash.addEventListener('change', update);
    rbEwallet.addEventListener('change', update);
    Array.prototype.forEach.call(services, function (box) {
        box.addEventListener('change', update);
    });
    Array.prototype.forEach.call(products, function (row) {
        qtyBox(row).addEventListener('input', update);
        Array.prototype.forEach.call(row.querySelectorAll('[data-qty-step]'), function (btn) {
            btn.addEventListener('click', function () {
                var step = parseInt(btn.getAttribute('data-qty-step'), 10);
                var next = qtyOf(row) + step;
                if (next < 0) { next = 0; }
                qtyBox(row).value = next;
                update();
            });
        });
    });
    document.querySelectorAll('[data-cash]').forEach(function (b) {
        b.addEventListener('click', function () {
            cash.value = parseFloat(b.getAttribute('data-cash')).toFixed(2);
            update();
        });
    });
    update();
})();

// Admin/EditJob.aspx
// Reprices the corrected job as the owner types, with the prices frozen on the job (the page puts
// them in data- attributes), and shows what the new total means: a balance still owed (collected
// before pick-up), an overpayment to refund now, or nothing.
// Display only: on save the server prices the job again (JobOrderService.QuoteEdit) and refuses the
// save if the amount is not the one shown here (hfShownDifference), so the owner never refunds a
// wrong amount.
(function () {
    var card = document.getElementById('loadCard');
    if (!card) return;

    var rates = {};                                                      // service id -> rate per kg
    card.getAttribute('data-rates').split(',').forEach(function (pair) {
        var p = pair.split(':');
        rates[p[0]] = parseFloat(p[1]);
    });
    var minimum = parseFloat(card.getAttribute('data-minimum'));        // frozen on the job
    var rounding = parseFloat(card.getAttribute('data-rounding'));      // frozen on the job
    var maxWeight = parseFloat(card.getAttribute('data-max-weight'));   // from Pricing
    var paid = parseFloat(card.getAttribute('data-paid'));              // net of refunds

    var serviceBox = document.getElementById('ddlService');
    var weightBox = document.getElementById('txtWeight');
    var weightError = document.getElementById('weightError');
    var services = document.querySelectorAll('[data-service]');
    var products = document.querySelectorAll('[data-product]');
    var shownDiff = document.getElementById('hfShownDifference');
    var saveBtn = document.getElementById('btnSave');
    var sumLines = document.getElementById('sumLines');

    function $(id) { return document.getElementById(id); }

    function peso(n) {
        if (!isFinite(n)) { return '—'; }
        return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    // Money is worked in centavos so 500.00 - 289.99 never comes out as 210.00999...
    function cents(n) { return Math.round(n * 100); }

    function kg(n) {
        return (Math.round(n * 100) / 100).toString() + ' kg';
    }

    function qtyOf(row) {
        var n = parseInt(row.querySelector('[data-qty]').value, 10);
        return isNaN(n) || n < 0 ? 0 : n;
    }

    // Each product row keeps its own line total; one at zero is dimmed.
    function paintProducts() {
        Array.prototype.forEach.call(products, function (row) {
            var qty = qtyOf(row);
            row.className = 'product-row ' + (qty === 0 ? 'is-zero' : 'is-on');
            row.querySelector('.line-total').textContent = peso(qty * parseFloat(row.getAttribute('data-price')));
            row.querySelector('[data-qty-step="-1"]').disabled = qty === 0;
        });
    }

    // The same sum as PricingService: weight rounded up to the step, times the rate, raised to the
    // minimum, then the add-on lines. Returns null while the weight is not a usable number.
    function quote() {
        var text = weightBox.value.trim().replace(',', '.');
        var weight = /^\d{1,3}(\.\d{1,2})?$/.test(text) ? parseFloat(text) : NaN;
        var tooHeavy = weight > maxWeight;
        var ok = weight > 0 && !tooHeavy;
        weightError.textContent = tooHeavy
            ? 'One job can be at most ' + kg(maxWeight) + '. Split a bigger load into two or more jobs.'
            : 'Enter the weight in kilos, for example 5.1.';
        weightError.hidden = ok;
        if (!ok) { return null; }

        var rate = rates[serviceBox.value];
        // Worked in thousandths so 3.0 kg / 0.5 does not come out as 6.0000001 steps.
        var billable = Math.ceil(Math.round(weight / rounding * 1000) / 1000) * rounding;
        var charge = cents(billable * rate) / 100;
        var laundry = Math.max(charge, minimum);

        var lines = [];
        Array.prototype.forEach.call(services, function (box) {
            if (box.checked) {
                lines.push({ label: box.getAttribute('data-name'), amount: parseFloat(box.getAttribute('data-price')) });
            }
        });
        Array.prototype.forEach.call(products, function (row) {
            var qty = qtyOf(row);
            if (qty > 0) {
                lines.push({ label: row.getAttribute('data-name') + ' × ' + qty, amount: qty * parseFloat(row.getAttribute('data-price')) });
            }
        });
        var total = lines.reduce(function (sum, line) { return sum + cents(line.amount); }, cents(laundry)) / 100;
        return { rate: rate, weight: weight, billable: billable, charge: charge, laundry: laundry, lines: lines, total: total };
    }

    function paintSummary(q) {
        $('sumRate').textContent = q ? peso(q.rate) + ' / kg' : '—';
        $('sumWeight').textContent = q ? kg(q.weight) + ' → billable' : 'Weight → billable';
        $('sumBillable').textContent = q ? kg(q.billable) : '—';
        $('sumLaundryLabel').textContent = q && q.laundry > q.charge ? 'Raised to minimum' : 'Laundry charge';
        $('sumLaundry').textContent = q ? peso(q.laundry) : '—';
        $('sumTotal').textContent = q ? peso(q.total) : '—';

        Array.prototype.forEach.call(sumLines.querySelectorAll('.addon-line'), function (el) {
            sumLines.removeChild(el);
        });
        (q ? q.lines : []).forEach(function (line) {
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

    // Higher total than paid: a balance, collected before pick-up (nothing is taken now). Lower: the
    // customer overpaid, so the excess is refunded now. Same: paid in full.
    function paintSettle(q) {
        var diff = q ? (cents(q.total) - cents(paid)) / 100 : 0;
        var state = !q || diff === 0 ? 'none' : diff > 0 ? 'balance' : 'refund';
        var amount = Math.abs(diff);
        shownDiff.value = q ? diff.toFixed(2) : '';

        $('settleCard').setAttribute('data-state', state);
        var pill = $('settlePill');
        pill.className = 'pill ' + (state === 'refund' ? 'pill-danger' : state === 'balance' ? 'pill-warn' : 'pill-ok');
        pill.textContent = state === 'refund' ? 'Overpaid' : state === 'balance' ? 'Balance due' : 'Paid in full';
        $('settleLine').style.color = state === 'refund' ? 'var(--danger)' : state === 'balance' ? 'var(--warning)' : 'var(--text)';
        $('settleLabel').textContent = state === 'refund' ? 'Refund to customer' : state === 'balance' ? 'Balance due' : 'Nothing owed';
        $('settleAmount').textContent = state === 'none' ? '' : peso(amount);

        $('balanceNotice').hidden = state !== 'balance';
        if (state === 'balance') {
            $('balanceText').textContent = 'The customer has paid ' + peso(paid) + ' of the ' + peso(q.total)
                + ' total. The ' + peso(amount) + ' balance is collected before pick-up, on the job page. Nothing is collected now.';
        }
        $('refundNotice').hidden = state !== 'refund';
        if (state === 'refund') {
            $('refundText').textContent = 'The customer paid ' + peso(paid) + ' but the corrected total is ' + peso(q.total)
                + '. Give back ' + peso(amount) + ' before saving. It is recorded as a −' + peso(amount) + ' refund row.';
        }
        $('methodField').hidden = state !== 'refund';
        $('saveLabel').textContent = state === 'refund' ? 'Save and refund ' + peso(amount) : 'Save changes';
        saveBtn.disabled = !q;
    }

    function update() {
        paintProducts();
        var q = quote();
        paintSummary(q);
        paintSettle(q);
    }

    serviceBox.addEventListener('change', update);
    weightBox.addEventListener('input', update);
    Array.prototype.forEach.call(services, function (box) {
        box.addEventListener('change', update);
    });
    Array.prototype.forEach.call(products, function (row) {
        row.querySelector('[data-qty]').addEventListener('input', update);
        Array.prototype.forEach.call(row.querySelectorAll('[data-qty-step]'), function (btn) {
            btn.addEventListener('click', function () {
                var next = qtyOf(row) + parseInt(btn.getAttribute('data-qty-step'), 10);
                row.querySelector('[data-qty]').value = next < 0 ? 0 : next;
                update();
            });
        });
    });

    // Enter would save the whole correction (the form's only submit button). A save that changes
    // what a customer owes should be a deliberate click, so Enter in a box does nothing here.
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && e.target.tagName === 'INPUT') { e.preventDefault(); }
    });

    update();
})();

// Pages/Shared/NewJob.aspx
// Keeps the order summary and the change in step with what the cashier ticks and types.
// Display only: on save the server recomputes the total from the stored prices and checks
// the cash again, and those are the values that are stored.
(function () {
    var loadCard = document.getElementById('loadCard');
    var serviceBox = document.getElementById('ddlService');
    var weightBox = document.getElementById('txtWeight');
    var minimum = parseFloat(loadCard.getAttribute('data-minimum'));     // written by the page
    var rounding = parseFloat(loadCard.getAttribute('data-rounding'));
    var maxWeight = parseFloat(loadCard.getAttribute('data-max-weight'));  // set on the Pricing page
    var rates = {};                                                       // service value -> rate per kg
    loadCard.getAttribute('data-rates').split(',').forEach(function (pair) {
        var p = pair.split(':');
        rates[p[0]] = parseFloat(p[1]);
    });
    var routes = {};                                                      // service value -> work stages, e.g. "WD"
    loadCard.getAttribute('data-routes').split(',').forEach(function (pair) {
        var p = pair.split(':');
        routes[p[0]] = p[1];
    });
    var cash = document.getElementById('txtCashReceived');
    var rbCash = document.getElementById('rbCash');
    var rbEwallet = document.getElementById('rbEwallet');
    var createBtn = document.getElementById('btnCreate');
    var exactBtn = document.getElementById('btnExact');
    var sumLines = document.getElementById('sumLines');
    var services = document.querySelectorAll('[data-service]');
    var products = document.querySelectorAll('[data-product]');

    // The parts of the page the price breakdown writes to, looked up once: the breakdown
    // runs on every keystroke in the weight box.
    var breakdown = {};
    ['weightError', 'weightHint', 'serviceRoute', 'sumService', 'sumRate', 'sumWeight', 'sumBillable',
     'sumLaundry', 'sumMinLabel', 'sumMin'].forEach(function (id) {
        breakdown[id] = document.getElementById(id);
    });

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

    // "WD" -> "Washing → Drying → Inspection". The route is copied onto the job when it is
    // saved, so the cashier sees here which stages this load will go through.
    function routeText(code) {
        var names = { W: 'Washing', D: 'Drying', F: 'Folding' };
        var steps = (code || '').split('').map(function (c) { return names[c]; });
        return steps.concat('Inspection').join(' → ');
    }

    function kg(n) {
        return (Math.round(n * 10) / 10).toFixed(1).replace(/\.0$/, '') + ' kg';
    }

    // The laundry charge, the way PricingService will work it out: round the weight up to the
    // next rounding step, times the service's rate, then raised to the minimum if lower.
    // Returns NaN when the weight is not a usable number.
    function laundryCharge() {
        var text = weightBox.value.trim().replace(',', '.');
        var weight = /^\d{1,3}(\.\d{1,2})?$/.test(text) ? parseFloat(text) : NaN;
        var tooHeavy = weight > maxWeight;
        var ok = weight > 0 && !tooHeavy;
        breakdown.weightError.textContent = tooHeavy
            ? 'One job can be at most ' + kg(maxWeight) + '. Split a bigger load into two or more jobs.'
            : 'Enter the weight in kilos, for example 5.1.';
        breakdown.weightError.hidden = ok || text === '';

        var option = serviceBox.options[serviceBox.selectedIndex];
        var rate = rates[option.value];
        breakdown.sumService.textContent = option.text.split(' · ')[0];
        breakdown.sumRate.textContent = peso(rate) + ' / kg';
        breakdown.serviceRoute.textContent = 'Stages: ' + routeText(routes[option.value]);

        if (!ok) {
            breakdown.weightHint.textContent = 'Weigh the load, then type the kilos shown on the scale.';
            breakdown.sumWeight.textContent = 'Weight → billable';
            breakdown.sumBillable.textContent = breakdown.sumLaundry.textContent = breakdown.sumMin.textContent = '—';
            return NaN;
        }

        // Worked in thousandths so 3.0 kg / 0.5 does not come out as 6.0000001 steps.
        var billable = Math.ceil(Math.round(weight / rounding * 1000) / 1000) * rounding;
        var charge = billable * rate;
        var raised = charge < minimum;

        breakdown.weightHint.textContent = 'Rounded up to the next ' + kg(rounding) + ': bills as ' + kg(billable) + '.';
        breakdown.sumWeight.textContent = kg(weight) + ' → billable';
        breakdown.sumBillable.textContent = kg(billable);
        breakdown.sumLaundry.textContent = peso(Math.max(charge, minimum));
        breakdown.sumMinLabel.textContent = 'Minimum ' + peso(minimum);
        breakdown.sumMin.textContent = raised ? 'applied (was ' + peso(charge) + ')' : 'not needed';
        breakdown.sumMin.className = 'small ' + (raised ? 'strong' : 'muted');
        return Math.max(charge, minimum);
    }

    function update() {
        paintProducts();

        var lines = addOnLines();
        paintSummary(lines);

        var laundry = laundryCharge();
        var total = lines.reduce(function (sum, line) { return sum + line.amount; }, laundry);
        document.getElementById('hfShownTotal').value = isFinite(total) ? total.toFixed(2) : '';
        document.getElementById('sumTotal').textContent = peso(total);
        document.getElementById('ewalletTotal').textContent = peso(total);
        exactBtn.setAttribute('data-cash', isFinite(total) ? total.toFixed(2) : '');
        exactBtn.disabled = !isFinite(total);   // no exact amount until there is a total

        var isCash = rbCash.checked;
        document.getElementById('cashFields').style.display = isCash ? '' : 'none';
        document.getElementById('cashSummary').style.display = isCash ? '' : 'none';
        document.getElementById('ewalletNote').style.display = isCash ? 'none' : '';

        // No total without a weight: say what is missing rather than print NaN, and hold the
        // save until there is a price to collect.
        if (!isFinite(total)) {
            paintChange('idle', 'Change', 'Enter the weight first');
            document.getElementById('sumReceived').textContent = '—';
            document.getElementById('sumChange').textContent = '—';
            createBtn.disabled = true;
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
    serviceBox.addEventListener('change', update);
    weightBox.addEventListener('input', update);
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
            var amount = parseFloat(b.getAttribute('data-cash'));
            if (isNaN(amount)) { return; }
            cash.value = amount.toFixed(2);
            update();
        });
    });
    update();
})();

// Pages/Shared/NewJob.aspx
// Keeps the order summary, the amount paid now, the balance and the change in step with what
// the cashier ticks and types. Display only: on save the server recomputes the total from the
// stored prices, checks the down payment against the owner's minimum and the cash again, and
// those are the values that are stored.
(function () {
    var loadCard = document.getElementById('loadCard');
    var serviceBox = document.getElementById('ddlService');
    if (!loadCard || !serviceBox || serviceBox.options.length === 0) return;   // no service to price yet
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
    // How much is paid now. The down payment and pay-at-pick-up choices are only on the page
    // when the owner's minimum allows them (Pricing), so either may be missing.
    var rbPayFull = document.getElementById('rbPayFull');
    var rbPayPart = document.getElementById('rbPayPart');
    var rbPayLater = document.getElementById('rbPayLater');
    var downBox = document.getElementById('txtDownPayment');
    var minPercent = parseInt(document.getElementById('paymentCard').getAttribute('data-min-percent'), 10) || 0;
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
        return (Math.round(n * 100) / 100).toString() + ' kg';   // up to two decimals: 5.15 kg shows as 5.15
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

        // Worked in hundredths of a kilo, as whole numbers, so a 0.1 kg step never leaves a float
        // remainder (3 x 0.1 is 0.30000000000000004 as a fraction, but exactly 30 as hundredths).
        var stepC = Math.round(rounding * 100);
        var billableC = Math.ceil(Math.round(weight * 100) / stepC) * stepC;
        var billable = billableC / 100;
        // The charge in whole centavos, from whole numbers only: hundredths of a kilo x centavos per kilo,
        // rounded half up, exactly as the server rounds (Math.Round(..., 2, AwayFromZero)).
        var chargeC = Math.floor((billableC * Math.round(rate * 100) + 50) / 100);
        var charge = chargeC / 100;
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

    // Money is worked in centavos so 500.00 - 289.99 never comes out as 210.00999...
    function cents(n) { return Math.round(n * 100); }

    function pesosTyped(box) {
        var typed = box.value.replace(/[,₱\s]/g, '');
        return typed === '' ? NaN : parseFloat(typed);
    }

    // The least the customer may pay now: the owner's percentage of the total, rounded up to the
    // centavo, the same as PaymentService.MinimumDownPayment.
    function minimumDown(total) {
        return Math.ceil(Math.round(total * 100) * minPercent / 100) / 100;
    }

    function update() {
        paintProducts();

        var lines = addOnLines();
        paintSummary(lines);

        var laundry = laundryCharge();
        var total = lines.reduce(function (sum, line) { return sum + line.amount; }, laundry);
        document.getElementById('sumTotal').textContent = peso(total);

        // What is paid now: everything, a down payment, or nothing (pay at pick-up).
        var plan = rbPayPart && rbPayPart.checked ? 'part' : rbPayLater && rbPayLater.checked ? 'later' : 'full';
        var minimum = isFinite(total) ? minimumDown(total) : NaN;
        var down = pesosTyped(downBox);
        var paidNow = plan === 'full' ? total : plan === 'later' ? 0 : down;
        var downProblem = '';
        if (plan === 'part' && isFinite(total)) {
            if (isNaN(down)) { downProblem = 'Enter the down payment.'; }
            else if (cents(down) < cents(minimum)) { downProblem = 'At least ' + peso(minimum) + ' (' + minPercent + '% of the total).'; }
            else if (cents(down) > cents(total)) { downProblem = 'The down payment can\'t be more than the ' + peso(total) + ' total.'; }
        }
        var balance = isFinite(total) && isFinite(paidNow) ? (cents(total) - cents(paidNow)) / 100 : NaN;

        document.getElementById('downFields').hidden = plan !== 'part';
        document.getElementById('laterNote').hidden = plan !== 'later';
        document.getElementById('downHint').textContent = isFinite(minimum)
            ? 'At least ' + peso(minimum) + ' (' + minPercent + '% of the total). The rest is paid before the load is claimed.'
            : 'At least ' + minPercent + '% of the total. The rest is paid before the load is claimed.';
        var downError = document.getElementById('downError');
        downError.textContent = downProblem;
        downError.hidden = downProblem === '' || isNaN(down);

        var paysNow = plan !== 'later';
        var isCash = rbCash.checked;
        document.getElementById('methodField').hidden = !paysNow;
        document.getElementById('cashFields').style.display = paysNow && isCash ? '' : 'none';
        document.getElementById('cashSummary').style.display = paysNow && isCash ? '' : 'none';
        document.getElementById('ewalletNote').style.display = paysNow && !isCash ? '' : 'none';
        document.getElementById('balanceSummary').hidden = plan === 'full';
        document.getElementById('sumPaidNow').textContent = isFinite(paidNow) ? peso(paidNow) : '—';
        document.getElementById('sumBalance').textContent = isFinite(balance) ? peso(balance) : '—';

        var amountOk = isFinite(total) && isFinite(paidNow) && downProblem === '';
        document.getElementById('ewalletTotal').textContent = amountOk ? peso(paidNow) : '—';
        exactBtn.setAttribute('data-cash', amountOk ? paidNow.toFixed(2) : '');
        exactBtn.disabled = !amountOk;   // no exact amount until there is one to pay

        var pill = document.getElementById('sumMethod');
        pill.className = 'pill ' + (plan === 'full' ? 'pill-ok' : plan === 'part' ? 'pill-warn' : 'pill-danger');
        pill.textContent = plan === 'later' ? 'Unpaid · pay at pick-up'
            : (plan === 'full' ? 'Paid in full · ' : 'Down payment · ') + (isCash ? 'Cash' : 'E-wallet');

        // No total without a weight: say what is missing rather than print NaN, and hold the
        // save until there is a price to collect.
        if (!isFinite(total)) {
            paintChange('idle', 'Change', 'Enter the weight first');
            document.getElementById('sumReceived').textContent = '—';
            document.getElementById('sumChange').textContent = '—';
            createBtn.disabled = true;
            return;
        }

        var received = pesosTyped(cash);
        var change = (cents(received) - cents(paidNow)) / 100;

        var state;
        if (!paysNow || !isCash) { state = 'ok'; }
        else if (!amountOk) { state = 'waiting'; }
        else if (isNaN(received)) { state = 'idle'; }
        else if (change < 0) { state = 'short'; }
        else if (change === 0) { state = 'exact'; }
        else { state = 'ok'; }

        if (state === 'waiting') { paintChange('idle', 'Change', 'Enter the down payment first'); }
        else if (state === 'idle') { paintChange('idle', 'Change', 'Enter cash received'); }
        else if (state === 'short') { paintChange('short', 'Short by', peso(-change)); }
        else if (state === 'exact') { paintChange('exact', 'Exact amount', 'No change'); }
        else if (isCash && paysNow) { paintChange('', 'Change', peso(change)); }

        var cashShown = paysNow && isCash && (state === 'ok' || state === 'exact' || state === 'short');
        document.getElementById('sumReceived').textContent = cashShown ? peso(received) : '—';
        document.getElementById('sumChange').textContent = cashShown && state !== 'short' ? peso(change) : '—';
        createBtn.disabled = !amountOk || (paysNow && isCash && state !== 'ok' && state !== 'exact');
        paintConfirm(lines, total, plan, paidNow, balance, isCash, received, change);
    }

    // The question Confirm.js asks before the job is saved: the whole order in a few lines, so the
    // cashier can read it back to the customer before pressing Continue.
    function paintConfirm(lines, total, plan, paidNow, balance, isCash, received, change) {
        var name = document.getElementById('txtCustomerName').value.trim() || 'New customer';
        var contact = document.getElementById('txtSearchContact').value.trim();
        var paying;
        if (plan === 'later') {
            paying = 'Nothing paid now. The whole ' + peso(total) + ' is paid before pick-up.';
        } else {
            paying = (plan === 'full' ? 'Paid in full, ' : 'Down payment ' + peso(paidNow) + ', ')
                + (isCash ? 'cash ' + peso(received) + ', change ' + peso(change) : 'e-wallet, exactly ' + peso(paidNow))
                + (plan === 'part' ? '.\nBalance ' + peso(balance) + ', paid before pick-up.' : '.');
        }
        var text = name + (contact ? ' · ' + contact : '') + '\n'
            + breakdown.sumService.textContent + ', ' + breakdown.sumWeight.textContent.replace(' → billable', '')
            + ' (billed as ' + breakdown.sumBillable.textContent + ')\n'
            + (lines.length ? lines.map(function (line) { return line.label; }).join(', ') + '\n' : '')
            + '\nTotal ' + peso(total) + '\n' + paying
            + '\n\nThe prices are saved with the job and never change later. The claim number is given when you continue.';
        createBtn.setAttribute('data-confirm-text', text);
    }

    function paintChange(state, label, amount) {
        document.getElementById('changeBox').className = 'change-box' + (state ? ' ' + state : '');
        document.getElementById('changeLabel').textContent = label;
        document.getElementById('changeAmount').textContent = amount;
    }

    cash.addEventListener('input', update);
    downBox.addEventListener('input', update);
    [rbPayFull, rbPayPart, rbPayLater].forEach(function (rb) {
        if (rb) { rb.addEventListener('change', update); }
    });
    document.getElementById('txtCustomerName').addEventListener('input', update);
    document.getElementById('txtSearchContact').addEventListener('input', update);
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

    // Enter in the contact number box presses Find (the form's first button). Anywhere else it
    // would do the same, which is never what the cashier meant, so there it does nothing.
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && e.target.tagName === 'INPUT' && !e.target.hasAttribute('data-find-box')) {
            e.preventDefault();
        }
    });
    update();
})();

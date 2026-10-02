(function () {
    var loadCard = document.getElementById('loadCard');
    var serviceBox = document.getElementById('ddlService');
    if (!loadCard || !serviceBox || serviceBox.options.length === 0) return;
    var weightBox = document.getElementById('txtWeight');
    var minimum = parseFloat(loadCard.getAttribute('data-minimum'));
    var maxWeight = parseFloat(loadCard.getAttribute('data-max-weight'));
    var rates = {};
    loadCard.getAttribute('data-rates').split(',').forEach(function (pair) {
        var p = pair.split(':');
        rates[p[0]] = parseFloat(p[1]);
    });
    var routes = {};
    loadCard.getAttribute('data-routes').split(',').forEach(function (pair) {
        var p = pair.split(':');
        routes[p[0]] = p[1];
    });
    var cash = document.getElementById('txtCashReceived');
    var rbCash = document.getElementById('rbCash');
    var rbEwallet = document.getElementById('rbEwallet');
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

    var breakdown = {};
    ['weightError', 'weightHint', 'serviceRoute', 'sumService', 'sumRate', 'sumWeight',
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

    function qtyOf(row) {
        var n = parseInt(qtyBox(row).value, 10);
        if (isNaN(n) || n < 0) { n = 0; }
        return n;
    }

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

    function paintProducts() {
        Array.prototype.forEach.call(products, function (row) {
            var qty = qtyOf(row);
            var price = parseFloat(row.getAttribute('data-price'));
            row.className = 'product-row ' + (qty === 0 ? 'is-zero' : 'is-on');
            row.querySelector('.line-total').textContent = peso(qty * price);
            row.querySelector('[data-qty-step="-1"]').disabled = qty === 0;
        });
    }

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

    function routeText(code) {
        var names = { W: 'Washing', D: 'Drying', F: 'Folding' };
        var steps = (code || '').split('').map(function (c) { return names[c]; });
        return ['Intake inspection'].concat(steps, 'Inspection').join(' → ');
    }

    function kg(n) {
        return (Math.round(n * 100) / 100).toString() + ' kg';
    }

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
            breakdown.sumWeight.textContent = breakdown.sumLaundry.textContent = breakdown.sumMin.textContent = '—';
            return NaN;
        }

        var weightC = Math.round(weight * 100);
        var chargeC = Math.floor((weightC * Math.round(rate * 100) + 50) / 100);
        var charge = chargeC / 100;
        var raised = charge < minimum;

        breakdown.weightHint.textContent = kg(weight) + ' × ' + peso(rate) + ' = ' + peso(charge) + '.';
        breakdown.sumWeight.textContent = kg(weight);
        breakdown.sumLaundry.textContent = peso(Math.max(charge, minimum));
        breakdown.sumMinLabel.textContent = 'Minimum ' + peso(minimum);
        breakdown.sumMin.textContent = raised ? 'applied (was ' + peso(charge) + ')' : 'not needed';
        breakdown.sumMin.className = 'small ' + (raised ? 'strong' : 'muted');
        return Math.max(charge, minimum);
    }

    function cents(n) { return Math.round(n * 100); }

    function pesosTyped(box) {
        var typed = box.value.replace(/[,₱\s]/g, '');
        return typed === '' ? NaN : parseFloat(typed);
    }

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
        exactBtn.disabled = !amountOk;

        var pill = document.getElementById('sumMethod');
        pill.className = 'pill ' + (plan === 'full' ? 'pill-ok' : plan === 'part' ? 'pill-warn' : 'pill-danger');
        pill.textContent = plan === 'later' ? 'Unpaid · pay at pick-up'
            : (plan === 'full' ? 'Paid in full · ' : 'Down payment · ') + (isCash ? 'Cash' : 'E-wallet');

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
            + breakdown.sumService.textContent + ', ' + breakdown.sumWeight.textContent + '\n'
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

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && e.target.tagName === 'INPUT' && !e.target.hasAttribute('data-find-box')) {
            e.preventDefault();
        }
    });
    update();
})();

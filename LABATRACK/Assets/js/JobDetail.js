// Pages/Shared/JobDetail.aspx
// 1. The page is one server form with several buttons. Left alone, Enter in a text box presses the
//    form's first button, which would move the job on to its next stage. Here Enter presses only the
//    button beside the box (Void job next to the void reason, Send back next to the rework reason)
//    and does nothing anywhere else.
// 2. Keeps the "are you sure?" questions of Void and Send back (Confirm.js) in step with the stage
//    and reason typed, so the window says exactly what will be saved.
// 3. The payment forms (take a payment; collect the balance and mark claimed): shows the change as
//    the cash is typed, hides the cash box for e-wallet, holds the button until the amount works,
//    and writes the amounts into the question.
// Display only: the server still checks every action, whatever is pressed.
(function () {
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter' || e.target.tagName !== 'INPUT') return;
        e.preventDefault();
        var scope = e.target.closest('[data-enter-scope]');
        var button = scope && scope.querySelector('button[type="submit"]');
        if (button) button.click();
    });

    function reasonLine(box) {
        var reason = box ? box.value.trim() : '';
        return reason === '' ? '\nNo reason typed yet: one is required.' : '\nReason: ' + reason;
    }

    var voidButton = document.querySelector('button[value="void"]');
    var voidReason = document.getElementById('txtVoidReason');
    if (voidButton && voidReason) {
        var voidBase = voidButton.getAttribute('data-confirm-text');
        var paintVoid = function () { voidButton.setAttribute('data-confirm-text', voidBase + reasonLine(voidReason)); };
        voidReason.addEventListener('input', paintVoid);
        paintVoid();
    }

    var reworkButton = document.getElementById('btnRework');
    var reworkStage = document.getElementById('ddlReworkStage');
    var reworkReason = document.getElementById('txtReworkReason');
    if (reworkButton && reworkStage && reworkReason) {
        var paintRework = function () {
            reworkButton.setAttribute('data-confirm-text',
                'Back to ' + reworkStage.value + ' to be redone, then through inspection again.' + reasonLine(reworkReason)
                + '\nThe timeline keeps both passes.');
        };
        reworkStage.addEventListener('change', paintRework);
        reworkReason.addEventListener('input', paintRework);
        paintRework();
    }

    function peso(n) {
        return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    // Money is worked in centavos so 500.00 - 289.99 never comes out as 210.00999...
    function cents(n) { return Math.round(n * 100); }

    function pesosTyped(box) {
        var typed = box.value.replace(/[,₱\s]/g, '');
        return typed === '' ? NaN : parseFloat(typed);
    }

    // data-what="pay": part or all of the balance, typed in data-pay-amount.
    // data-what="claim": the whole balance (data-balance), then the load is handed over.
    Array.prototype.forEach.call(document.querySelectorAll('[data-pay-form]'), function (form) {
        var balance = parseFloat(form.getAttribute('data-balance'));
        var isClaim = form.getAttribute('data-what') === 'claim';
        var amountBox = form.querySelector('[data-pay-amount]');
        var cashBox = form.querySelector('[data-pay-cash]');
        var methods = form.querySelectorAll('[data-pay-method]');
        var submit = form.querySelector('[data-pay-submit]');
        var box = form.querySelector('[data-change-box]');

        function paint() {
            var amount = isClaim ? balance : pesosTyped(amountBox);
            var amountOk = !isNaN(amount) && cents(amount) > 0 && cents(amount) <= cents(balance);
            var isCash = form.querySelector('[data-pay-method]:checked').value === 'Cash';
            var received = pesosTyped(cashBox);
            var change = (cents(received) - cents(amount)) / 100;

            form.querySelector('[data-cash-field]').hidden = !isCash;
            box.hidden = !isCash;
            var label = 'Change', text = 'Enter cash received', css = 'idle';
            if (!amountOk) { text = cents(amount) > cents(balance) ? 'More than the ' + peso(balance) + ' balance' : 'Enter the amount first'; css = cents(amount) > cents(balance) ? 'short' : 'idle'; }
            else if (isNaN(received)) { /* nothing typed yet */ }
            else if (change < 0) { label = 'Short by'; text = peso(-change); css = 'short'; }
            else if (change === 0) { label = 'Exact amount'; text = 'No change'; css = 'exact'; }
            else { text = peso(change); css = ''; }
            box.className = 'change-box' + (css ? ' ' + css : '');
            box.querySelector('[data-change-label]').textContent = label;
            box.querySelector('[data-change-amount]').textContent = text;

            var cashOk = !isCash || (!isNaN(received) && change >= 0);
            submit.disabled = !amountOk || !cashOk;
            if (!amountOk) { return; }

            var how = isCash ? 'cash ' + peso(received) + ', change ' + peso(change) : 'e-wallet, exactly ' + peso(amount);
            var left = (cents(balance) - cents(amount)) / 100;
            submit.setAttribute('data-confirm-text', form.getAttribute('data-customer') + '\n\n'
                + (isClaim
                    ? 'Balance ' + peso(amount) + ' paid now: ' + how + '.\nThen the load is handed over and the job closes as Claimed. Claimed is final.'
                    : peso(amount) + ' paid now: ' + how + '.\n'
                      + (left > 0 ? 'Still owed after this: ' + peso(left) + ', collected when the load is claimed.' : 'After this the job is paid in full.')));
        }

        if (amountBox) { amountBox.addEventListener('input', paint); }
        cashBox.addEventListener('input', paint);
        Array.prototype.forEach.call(methods, function (m) { m.addEventListener('change', paint); });
        paint();
    });
})();

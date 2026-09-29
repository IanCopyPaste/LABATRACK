// Pages/Shared/JobDetail.aspx
// 1. The page is one server form with several buttons. Left alone, Enter in a text box presses the
//    form's first button, which would move the job on to its next stage. Here Enter presses only the
//    button beside the box (Void job next to the void reason, Send back next to the rework reason)
//    and does nothing anywhere else.
// 2. Keeps the "are you sure?" questions of Void and Send back (Confirm.js) in step with the stage
//    and reason typed, so the window says exactly what will be saved.
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
})();

// Shared: asks before a button that changes something for good (create a job, move a load to
// another stage, void, delete an account).
// Such a button carries data-confirm-title, and may carry data-confirm-text (what will happen),
// data-confirm-ok (the label of the button that goes ahead) and data-confirm-danger (a red one).
// The first press opens a small window with that text. Continue presses the button for real;
// Cancel, Esc or a click outside the window does nothing. Display only: the server checks every
// rule again, whatever is pressed.
(function () {
    var dialog = null, titleEl, textEl, okButton, cancelButton, pending = null;

    function build() {
        dialog = document.createElement('dialog');
        dialog.className = 'modal confirm';
        dialog.setAttribute('aria-labelledby', 'confirmTitle');
        dialog.innerHTML =
            '<div class="modal-h"><div><h2 id="confirmTitle"></h2></div></div>' +
            '<div class="modal-b"><p class="confirm-text"></p></div>' +
            '<div class="modal-f">' +
            '<button type="button" class="btn btn-ghost" data-confirm-cancel="true">Cancel</button>' +
            '<button type="button" class="btn btn-primary" data-confirm-go="true">Continue</button>' +
            '</div>';
        document.body.appendChild(dialog);

        titleEl = dialog.querySelector('h2');
        textEl = dialog.querySelector('.confirm-text');
        okButton = dialog.querySelector('[data-confirm-go]');
        cancelButton = dialog.querySelector('[data-confirm-cancel]');

        cancelButton.addEventListener('click', function () { dialog.close(); });
        okButton.addEventListener('click', function () {
            var button = pending;
            dialog.close();
            if (button) {
                button.setAttribute('data-confirmed', 'yes');
                button.click();
            }
        });
        // A click on the dimmed backdrop (outside the window) is the same as Cancel.
        dialog.addEventListener('click', function (e) {
            if (e.target === dialog) dialog.close();
        });
        dialog.addEventListener('close', function () { pending = null; });
    }

    function ask(button) {
        if (!dialog) build();
        pending = button;
        titleEl.textContent = button.getAttribute('data-confirm-title');
        textEl.textContent = button.getAttribute('data-confirm-text') || '';
        textEl.hidden = textEl.textContent === '';
        okButton.textContent = button.getAttribute('data-confirm-ok') || 'Continue';
        var danger = button.hasAttribute('data-confirm-danger');
        okButton.className = 'btn ' + (danger ? 'btn-danger-solid' : 'btn-primary');
        dialog.showModal();
        // For something that cannot be undone, Enter should not go ahead by accident.
        (danger ? cancelButton : okButton).focus();
    }

    // Caught on the way down (capture), so the question comes before anything else the button does.
    document.addEventListener('click', function (e) {
        var button = e.target.closest('[data-confirm-title]');
        if (!button || button.disabled) return;
        if (button.getAttribute('data-confirmed') === 'yes') {
            button.removeAttribute('data-confirmed');   // the real press, after Continue
            return;
        }
        e.preventDefault();
        e.stopPropagation();
        ask(button);
    }, true);
})();

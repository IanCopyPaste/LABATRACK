// Shared: every page.
// Gives a little feedback while the server works, so a press never looks like nothing happened:
//   - the button that was pressed shows a spinner and stays as wide as it was (class "is-loading");
//   - a thin bar slides along the top of the window (also when a link opens another page);
//   - a second press of any button is ignored while the first is still being sent, so a payment or a
//     job can never be sent twice by a double click.
// The button is NOT disabled: a disabled button is left out of the form that is sent, and the server
// reads which button was pressed (name="action"). Display only: every rule is still checked again on
// the server. If the server does not answer within 30 seconds the page is released again.
(function () {
    var html = document.documentElement;
    var bar = null, pressed = null, releaseTimer = null;

    function showBar() {
        if (!bar) {
            bar = document.createElement('div');
            bar.className = 'page-progress';
            bar.setAttribute('aria-hidden', 'true');
            document.body.appendChild(bar);
        }
        bar.classList.add('is-on');
    }

    function startLoading(button) {
        html.classList.add('is-busy');
        showBar();
        if (button && button.classList) {
            // Keep the button as wide as it was, so the spinner does not make it jump.
            button.style.minWidth = Math.ceil(button.getBoundingClientRect().width) + 'px';
            button.classList.add('is-loading');
            button.setAttribute('aria-busy', 'true');
            pressed = button;
        }
        clearTimeout(releaseTimer);
        releaseTimer = setTimeout(stopLoading, 30000);
    }

    function stopLoading() {
        clearTimeout(releaseTimer);
        html.classList.remove('is-busy');
        if (bar) bar.classList.remove('is-on');
        if (pressed) {
            pressed.classList.remove('is-loading');
            pressed.removeAttribute('aria-busy');
            pressed.style.minWidth = '';
            pressed = null;
        }
    }

    // A form is being sent. e.submitter is the button that was pressed (or the one Enter stands for).
    document.addEventListener('submit', function (e) {
        if (e.defaultPrevented) return;             // something else stopped it (for example a check that failed)
        if (html.classList.contains('is-busy')) {   // already being sent: ignore the second press
            e.preventDefault();
            return;
        }
        var button = e.submitter;
        if (!button && document.activeElement && document.activeElement.type === 'submit') button = document.activeElement;
        startLoading(button);
    });

    // A link to another page of this site: only the bar, since there is no button to spin.
    document.addEventListener('click', function (e) {
        if (e.defaultPrevented || e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;
        var link = e.target.closest ? e.target.closest('a[href]') : null;
        if (!link || (link.target && link.target !== '_self') || link.hasAttribute('download')) return;
        if (link.protocol !== 'http:' && link.protocol !== 'https:') return;
        if (link.origin !== location.origin) return;
        if (link.pathname === location.pathname && link.search === location.search) return;   // same page, or only a #section
        showBar();
        clearTimeout(releaseTimer);
        releaseTimer = setTimeout(stopLoading, 30000);
    });

    // Coming back to this page with the Back button can show it as it was left, still "loading".
    window.addEventListener('pageshow', function (e) {
        if (e.persisted) stopLoading();
    });
})();

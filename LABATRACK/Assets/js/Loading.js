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

    document.addEventListener('submit', function (e) {
        if (e.defaultPrevented) return;
        if (html.classList.contains('is-busy')) {
            e.preventDefault();
            return;
        }
        var button = e.submitter;
        if (!button && document.activeElement && document.activeElement.type === 'submit') button = document.activeElement;
        startLoading(button);
    });

    document.addEventListener('click', function (e) {
        if (e.defaultPrevented || e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;
        var link = e.target.closest ? e.target.closest('a[href]') : null;
        if (!link || (link.target && link.target !== '_self') || link.hasAttribute('download')) return;
        if (link.protocol !== 'http:' && link.protocol !== 'https:') return;
        if (link.origin !== location.origin) return;
        if (link.pathname === location.pathname && link.search === location.search) return;
        showBar();
        clearTimeout(releaseTimer);
        releaseTimer = setTimeout(stopLoading, 30000);
    });

    window.addEventListener('pageshow', function (e) {
        if (e.persisted) stopLoading();
    });
})();

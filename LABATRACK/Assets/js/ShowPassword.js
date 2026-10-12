(function () {
    function setShown(box, button, shown) {
        box.type = shown ? 'text' : 'password';
        button.setAttribute('aria-pressed', shown ? 'true' : 'false');
        button.setAttribute('aria-label', shown ? 'Hide password' : 'Show password');
        button.title = shown ? 'Hide password' : 'Show password';
    }

    document.addEventListener('click', function (e) {
        var button = e.target.closest('[data-reveal]');
        if (!button) return;
        var box = document.getElementById(button.getAttribute('data-reveal'));
        if (!box) return;
        setShown(box, button, box.type === 'password');
        box.focus();
    });

    document.addEventListener('submit', function () {
        Array.prototype.forEach.call(document.querySelectorAll('[data-reveal]'), function (button) {
            var box = document.getElementById(button.getAttribute('data-reveal'));
            if (box) setShown(box, button, false);
        });
    });
})();

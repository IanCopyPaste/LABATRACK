(function () {
    function kindOf(el) {
        if (!el || el.tagName !== 'INPUT' || el.readOnly) return null;
        var mode = el.getAttribute('inputmode');
        return mode === 'decimal' || mode === 'numeric' || mode === 'tel' ? mode : null;
    }

    function clean(kind, text) {
        if (kind === 'numeric') return text.replace(/\D/g, '');
        if (kind === 'tel') return text.replace(/[^\d\s+\-()]/g, '');
        var out = '', seenDot = false;
        for (var i = 0; i < text.length; i++) {
            var ch = text.charAt(i);
            if (ch >= '0' && ch <= '9') { out += ch; }
            else if (ch === '.' && !seenDot) { out += ch; seenDot = true; }
        }
        return out;
    }

    document.addEventListener('beforeinput', function (e) {
        var kind = kindOf(e.target);
        if (!kind || e.inputType !== 'insertText' || !e.data) return;
        var box = e.target, start = box.selectionStart, end = box.selectionEnd;
        var next = box.value.slice(0, start) + e.data + box.value.slice(end);
        if (clean(kind, next) !== next) e.preventDefault();
    }, true);

    document.addEventListener('input', function (e) {
        var kind = kindOf(e.target);
        if (!kind) return;
        var box = e.target, text = box.value, cleaned = clean(kind, text);
        if (cleaned === text) return;
        var caret = clean(kind, text.slice(0, box.selectionStart)).length;
        box.value = cleaned;
        box.setSelectionRange(caret, caret);
    }, true);
})();

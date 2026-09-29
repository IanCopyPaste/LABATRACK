// Shared: pages with number boxes (New job, Edit job, Job page, Pricing, Customers).
// A box that takes a number keeps letters out. Which kind of number is read from the box's own
// inputmode, which every such box already has:
//   inputmode="decimal"  digits and one dot          (weight, pesos, cash)
//   inputmode="numeric"  digits only                 (quantities, percentages)
//   inputmode="tel"      digits, space + - ( )       (contact numbers)
// A wrong key is simply not typed, and pasted text is cleaned. Works for boxes added later too.
// Display only: the server checks every number again.
(function () {
    function kindOf(el) {
        if (!el || el.tagName !== 'INPUT' || el.readOnly) return null;
        var mode = el.getAttribute('inputmode');
        return mode === 'decimal' || mode === 'numeric' || mode === 'tel' ? mode : null;
    }

    // The text with everything that does not belong removed.
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

    // Typing: refuse a key that would leave something that does not belong, before it appears.
    document.addEventListener('beforeinput', function (e) {
        var kind = kindOf(e.target);
        if (!kind || e.inputType !== 'insertText' || !e.data) return;
        var box = e.target, start = box.selectionStart, end = box.selectionEnd;
        var next = box.value.slice(0, start) + e.data + box.value.slice(end);
        if (clean(kind, next) !== next) e.preventDefault();
    }, true);

    // Pasting, dropping, or autofill: clean whatever arrived and keep the cursor where it was.
    // Runs before the page's own input handlers (capture), so they only ever see clean text.
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

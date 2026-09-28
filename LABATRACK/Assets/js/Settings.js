// Admin/Settings.aspx
// The page is one server form holding several cards. Left alone, Enter in any box presses the
// form's first submit button, so Enter in "Shop name" would try to change the password.
// Here Enter presses only the Save button of the card the box is in.
(function () {
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter' || e.target.tagName !== 'INPUT') return;
        e.preventDefault();
        var card = e.target.closest('.card');
        var save = card && card.querySelector('.card-f .btn-primary');
        if (save) save.click();
    });
})();

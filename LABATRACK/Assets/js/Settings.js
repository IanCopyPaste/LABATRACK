(function () {
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter' || e.target.tagName !== 'INPUT') return;
        e.preventDefault();
        var card = e.target.closest('.card');
        var save = card && card.querySelector('.card-f .btn-primary');
        if (save) save.click();
    });
})();

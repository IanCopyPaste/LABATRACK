// Print buttons (JobSlip, DailySalesReport): any element with data-print opens the print dialog.
document.querySelectorAll('[data-print]').forEach(function (button) {
    button.addEventListener('click', function () {
        window.print();
    });
});

// FTP Management Web UI - Custom JavaScript

$(document).ready(function () {
    // Initialize Bootstrap tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize Bootstrap popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Auto-hide alerts after 5 seconds
    setTimeout(function () {
        $('.alert-dismissible').fadeOut('slow', function () {
            $(this).remove();
        });
    }, 5000);

    // Confirm delete actions
    $('a[href*="/Delete/"]').on('click', function (e) {
        if (!confirm('Are you sure you want to delete this item?')) {
            e.preventDefault();
            return false;
        }
    });

    // Form validation
    $('.needs-validation').on('submit', function (event) {
        if (this.checkValidity() === false) {
            event.preventDefault();
            event.stopPropagation();
        }
        $(this).addClass('was-validated');
    });

    // Password strength indicator
    $('input[type="password"]').on('keyup', function () {
        var password = $(this).val();
        var strength = 0;
        var feedback = '';
        var feedbackClass = '';

        if (password.length >= 8) strength++;
        if (password.match(/[a-z]+/)) strength++;
        if (password.match(/[A-Z]+/)) strength++;
        if (password.match(/[0-9]+/)) strength++;
        if (password.match(/[^a-zA-Z0-9]+/)) strength++;

        switch (strength) {
            case 0:
            case 1:
                feedback = 'Weak';
                feedbackClass = 'text-danger';
                break;
            case 2:
                feedback = 'Fair';
                feedbackClass = 'text-warning';
                break;
            case 3:
                feedback = 'Good';
                feedbackClass = 'text-info';
                break;
            case 4:
            case 5:
                feedback = 'Strong';
                feedbackClass = 'text-success';
                break;
        }

        var strengthIndicator = $(this).siblings('.password-strength');
        if (strengthIndicator.length === 0) {
            $(this).after('<div class="password-strength form-text"></div>');
            strengthIndicator = $(this).siblings('.password-strength');
        }

        if (password.length > 0) {
            strengthIndicator.html('Password strength: <span class="' + feedbackClass + '">' + feedback + '</span>');
        } else {
            strengthIndicator.html('');
        }
    });

    // Table row click to view details
    $('table.table-hover tbody tr').on('click', function (e) {
        // Don't trigger if clicking on a link or button
        if ($(e.target).closest('a, button').length === 0) {
            var firstLink = $(this).find('a').first();
            if (firstLink.length > 0) {
                window.location = firstLink.attr('href');
            }
        }
    });

    // Search form auto-submit on clear
    $('#search').on('search', function () {
        if ($(this).val() === '') {
            $(this).closest('form').submit();
        }
    });

    // Print functionality
    window.printReport = function () {
        window.print();
    };

    // Export table to CSV
    window.exportTableToCSV = function (tableId, filename) {
        var csv = [];
        var rows = document.querySelectorAll('#' + tableId + ' tr');

        for (var i = 0; i < rows.length; i++) {
            var row = [], cols = rows[i].querySelectorAll('td, th');

            for (var j = 0; j < cols.length; j++) {
                var text = cols[j].innerText.replace(/"/g, '""');
                row.push('"' + text + '"');
            }

            csv.push(row.join(','));
        }

        var csvFile = new Blob([csv.join('\n')], { type: 'text/csv' });
        var downloadLink = document.createElement('a');
        downloadLink.download = filename || 'export.csv';
        downloadLink.href = window.URL.createObjectURL(csvFile);
        downloadLink.style.display = 'none';
        document.body.appendChild(downloadLink);
        downloadLink.click();
        document.body.removeChild(downloadLink);
    };

    // Real-time clock for dashboard
    function updateClock() {
        var now = new Date();
        var timeString = now.toLocaleTimeString();
        $('.dashboard-clock').text(timeString);
    }

    if ($('.dashboard-clock').length > 0) {
        updateClock();
        setInterval(updateClock, 1000);
    }

    // Keyboard shortcuts
    $(document).on('keydown', function (e) {
        // Ctrl+N for new user
        if (e.ctrlKey && e.key === 'n') {
            e.preventDefault();
            window.location.href = '/Users/Create';
        }
        // Ctrl+S for save (in forms)
        if (e.ctrlKey && e.key === 's' && $('form').length > 0) {
            e.preventDefault();
            $('form').first().submit();
        }
    });

    // Copy to clipboard functionality
    $('.copy-to-clipboard').on('click', function () {
        var text = $(this).data('clipboard-text') || $(this).text();
        
        var tempInput = $('<textarea>');
        $('body').append(tempInput);
        tempInput.val(text).select();
        document.execCommand('copy');
        tempInput.remove();
        
        $(this).tooltip({
            title: 'Copied!',
            trigger: 'manual',
            placement: 'top'
        }).tooltip('show');
        
        setTimeout(() => {
            $(this).tooltip('hide');
        }, 2000);
    });
});

// Helper functions
window.FtpManagement = {
    // Show loading overlay
    showLoading: function () {
        if ($('#loading-overlay').length === 0) {
            $('body').append('<div id="loading-overlay" class="position-fixed top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center" style="background: rgba(0,0,0,0.5); z-index: 9999;"><div class="spinner-border text-light" role="status"><span class="visually-hidden">Loading...</span></div></div>');
        }
        $('#loading-overlay').fadeIn();
    },

    // Hide loading overlay
    hideLoading: function () {
        $('#loading-overlay').fadeOut();
    },

    // Show toast notification
    showToast: function (message, type) {
        var toastHtml = '<div class="toast align-items-center text-white bg-' + (type || 'info') + ' border-0" role="alert" aria-live="assertive" aria-atomic="true">' +
            '<div class="d-flex">' +
            '<div class="toast-body">' + message + '</div>' +
            '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>' +
            '</div>' +
            '</div>';
        
        if ($('.toast-container').length === 0) {
            $('body').append('<div class="toast-container position-fixed bottom-0 end-0 p-3"></div>');
        }
        
        var toastElement = $(toastHtml);
        $('.toast-container').append(toastElement);
        var toast = new bootstrap.Toast(toastElement[0]);
        toast.show();
        
        toastElement.on('hidden.bs.toast', function () {
            $(this).remove();
        });
    }
}; 
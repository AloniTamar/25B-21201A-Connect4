(function ($) {
    if (!$.validator) return;

    $.validator.setDefaults({
        highlight: function (element) {
            const $el = $(element);
            $el.addClass('is-invalid').removeClass('is-valid');
            if ($el.hasClass('form-select')) $el.addClass('is-invalid').removeClass('is-valid');
        },
        unhighlight: function (element) {
            const $el = $(element);
            $el.removeClass('is-invalid').addClass('is-valid');
            if ($el.hasClass('form-select')) $el.removeClass('is-invalid').addClass('is-valid');
        },
        errorClass: 'invalid-feedback',
        errorPlacement: function (error, element) {
            if (element.parent('.input-group').length) {
                error.insertAfter(element.parent());
            } else if (element.hasClass('form-check-input')) {
                error.appendTo(element.closest('.form-check, .mb-3'));
            } else {
                error.insertAfter(element);
            }
        }
    });
})(jQuery);

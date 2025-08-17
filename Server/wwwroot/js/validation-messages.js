(function ($) {
    if (!$.validator) return;

    $.extend($.validator.messages, {
        required: "Please fill out this field.",
        number: "Enter a valid number.",
        email: "Enter a valid email address.",
        minlength: $.validator.format("Enter at least {0} characters."),
        maxlength: $.validator.format("Use no more than {0} characters."),
        min: $.validator.format("Value must be at least {0}."),
        max: $.validator.format("Value must be at most {0}."),
        range: $.validator.format("Value must be between {0} and {1}.")
    });
})(jQuery);

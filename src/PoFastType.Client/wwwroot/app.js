// JavaScript functions for PoFastType

window.focusElement = function (element) {
    if (element) {
        // Add a small delay to ensure the element is ready
        setTimeout(function() {
            element.focus();
        }, 10); // 10ms delay
    }
};

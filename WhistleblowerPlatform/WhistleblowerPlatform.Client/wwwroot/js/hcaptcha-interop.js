window.registerBlazorCaptchaCallback = function (dotNetHelper) {
    window.blazorCaptchaCallback = dotNetHelper;
};

window.hCaptchaInterop = {
    /**
     * Shows the CAPTCHA modal
     */
    showModal: function () {
        const modalElement = document.getElementById('captchaModal');
        if (modalElement) {
            const modal = new bootstrap.Modal(modalElement, {
                backdrop: 'static',
                keyboard: false
            });

            // Show modal first
            modal.show();

            // Wait a moment for modal to be visible, then render hCaptcha
            setTimeout(function () {
                try {
                    if (typeof hcaptcha !== 'undefined') {
                        // Find the hcaptcha container
                        const container = modalElement.querySelector('.h-captcha');
                        if (container && !container.hasAttribute('data-hcaptcha-widget-id')) {
                            hcaptcha.render(container, {
                                sitekey: container.getAttribute('data-sitekey'),
                                callback: 'onCaptchaSuccess',
                                'expired-callback': 'onCaptchaExpired',
                                'error-callback': 'onCaptchaError'
                            });
                        }
                    }
                } catch (error) {
                    console.error('Error rendering hCaptcha:', error);
                }
            }, 100);
        } else {
            console.error('CAPTCHA modal element not found');
        }
    },

    /**
     * Hides the CAPTCHA modal
     */
    hideModal: function () {
        const modalElement = document.getElementById('captchaModal');
        if (modalElement) {
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }
        }
    },

    /**
     * Resets the hCaptcha widget
     */
    reset: function () {
        try {
            if (typeof hcaptcha !== 'undefined') {
                hcaptcha.reset();
            }
        } catch (error) {
            console.error('Error resetting hCaptcha:', error);
        }
    },

    /**
     * Gets the current hCaptcha response token
     * Returns null if not solved
     */
    getResponse: function () {
        try {
            if (typeof hcaptcha !== 'undefined') {
                const response = hcaptcha.getResponse();
                return response || null;
            }
            return null;
        } catch (error) {
            console.error('Error getting hCaptcha response:', error);
            return null;
        }
    }
};

/**
 * Global callback when CAPTCHA is solved
 * Called by hCaptcha widget via data-callback attribute
 */
function onCaptchaSuccess(token) {
    // Notify Blazor component
    if (window.blazorCaptchaCallback) {
        window.blazorCaptchaCallback.invokeMethodAsync('OnCaptchaSuccess', token);
    }
}

/**
 * Global callback when CAPTCHA expires
 */
function onCaptchaExpired() {
    if (window.blazorCaptchaCallback) {
        window.blazorCaptchaCallback.invokeMethodAsync('OnCaptchaExpired');
    }
}

/**
 * Global callback when CAPTCHA encounters an error
 */
function onCaptchaError() {
    if (window.blazorCaptchaCallback) {
        window.blazorCaptchaCallback.invokeMethodAsync('OnCaptchaError');
    }
}
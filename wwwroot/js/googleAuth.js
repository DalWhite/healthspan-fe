window.healthspanAuth = (function () {
    let dotNetRef = null;
    let initialized = false;

    const ensureGoogleLoaded = () =>
        new Promise((resolve, reject) => {
            if (window.google?.accounts?.id) {
                resolve(true);
                return;
            }

            const maxAttempts = 40;
            let attempts = 0;
            const intervalId = setInterval(() => {
                attempts++;
                if (window.google?.accounts?.id) {
                    clearInterval(intervalId);
                    resolve(true);
                } else if (attempts >= maxAttempts) {
                    clearInterval(intervalId);
                    reject("GOOGLE_NOT_READY");
                }
            }, 100);
        });

    const initialize = async (reference, clientId) => {
        if (!clientId) {
            throw "GOOGLE_CLIENT_ID_MISSING";
        }

        await ensureGoogleLoaded();

        dotNetRef = reference;

        window.google.accounts.id.initialize({
            client_id: clientId,
            callback: (response) => {
                if (!response || !response.credential || !dotNetRef) {
                    return;
                }

                dotNetRef.invokeMethodAsync("HandleGoogleCredentialAsync", response.credential);
            },
            auto_select: false,
            context: "signin",
            ux_mode: "popup"
        });

        initialized = true;
    };

    const prompt = async () => {
        if (!initialized) {
            throw "GOOGLE_NOT_INITIALIZED";
        }

        window.google.accounts.id.prompt((notification) => {
            if (!dotNetRef || !notification) {
                return;
            }

            const dismissed = notification.isDismissedMoment?.();
            const skipped = notification.isSkippedMoment?.();
            const notDisplayed = notification.isNotDisplayed?.();

            if (dismissed || skipped || notDisplayed) {
                const reason =
                    dismissed
                        ? `dismissed:${notification.getDismissedReason?.() ?? "unknown"}`
                        : skipped
                            ? `skipped:${notification.getSkippedReason?.() ?? "unknown"}`
                            : `not_displayed:${notification.getNotDisplayedReason?.() ?? "unknown"}`;

                dotNetRef.invokeMethodAsync("HandleGooglePromptStatusAsync", reason);
            }
        });
    };

    const dispose = () => {
        dotNetRef = null;
        initialized = false;
    };

    return {
        initialize,
        prompt,
        dispose
    };
})();


(() => {
    const reconnectOptions = {
        maxRetries: 24,
        retryIntervalMilliseconds: 5000
    };

    const configureSignalR = (builder) => {
        builder
            .withServerTimeout(120000)
            .withKeepAliveInterval(15000);
    };

    let startPromise;

    const startBlazor = () => {
        if (startPromise || !window.Blazor) {
            return startPromise;
        }

        startPromise = window.Blazor.start({
            circuit: {
                configureSignalR
            },
            reconnectionOptions: reconnectOptions
        });

        return startPromise;
    };

    const reconnectWhenVisible = () => {
        if (document.visibilityState !== "visible"
            || !window.Blazor
            || typeof window.Blazor.reconnect !== "function") {
            return;
        }

        // Background tabs can pause the SignalR timers. Give the browser one
        // task turn to resume networking, then ask Blazor to reconnect now.
        window.setTimeout(() => {
            if (document.visibilityState !== "visible"
                || typeof window.Blazor.reconnect !== "function") {
                return;
            }

            Promise.resolve(window.Blazor.reconnect()).catch(() => {
                // The built-in reconnection UI/policy remains the source of
                // truth if the circuit is still unavailable.
            });
        }, 150);
    };

    document.addEventListener("visibilitychange", reconnectWhenVisible);
    window.addEventListener("pageshow", reconnectWhenVisible);
    startBlazor();
})();
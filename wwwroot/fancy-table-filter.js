const instances = new WeakMap();

export function show(anchor, popup, dotNet) {
    if (!anchor || !popup) return;

    dispose(popup);

    const abort = new AbortController();
    const signal = abort.signal;
    instances.set(popup, { abort });

    popup.addEventListener("toggle", event => {
        if (event.newState !== "closed") return;
        dispose(popup);
        anchor.focus();
        dotNet?.invokeMethodAsync("OnPopoverClosed").catch(() => { });
    }, { signal });

    const close = () => hide(popup);
    window.addEventListener("resize", close, { signal });
    window.addEventListener("scroll", event => {
        if (event.target instanceof Node && popup.contains(event.target)) return;
        close();
    }, { signal, capture: true });

    try {
        popup.showPopover();
    } catch (error) {
        if (error?.name !== "InvalidStateError") throw error;
    }

    const rect = anchor.getBoundingClientRect();
    const margin = 8;
    let left = Math.min(rect.left, window.innerWidth - margin - popup.offsetWidth);
    left = Math.max(left, margin);

    popup.style.position = "fixed";
    popup.style.inset = "auto";
    popup.style.margin = "0";
    popup.style.top = `${rect.bottom + 4}px`;
    popup.style.left = `${left}px`;

    popup.querySelector("input, select")?.focus();
}

export function hide(popup) {
    if (!popup) return;
    if (popup.matches(":popover-open")) {
        popup.hidePopover();
    }
}

export function dispose(popup) {
    const state = instances.get(popup);
    if (!state) return;
    state.abort.abort();
    instances.delete(popup);
}

const instances = new WeakMap();

export async function show(anchor, popup, dotNet) {
    if (!anchor || !popup) return;

    // The anchor's popovertarget opens the popover natively when its click finishes. Wait one task so
    // that activation has run; otherwise showPopover() here would be undone by the native toggle.
    await new Promise(resolve => setTimeout(resolve));

    dispose(popup);

    const abort = new AbortController();
    const signal = abort.signal;
    instances.set(popup, { abort });

    popup.addEventListener("toggle", event => {
        if (event.newState !== "closed") return;
        dispose(popup);
        const active = document.activeElement;
        if (!active || active === document.body || popup.contains(active)) {
            anchor.focus({ preventScroll: true });
        }
        dotNet?.invokeMethodAsync("OnPopoverClosed").catch(() => { });
    }, { signal });

    const close = () => hide(popup);
    window.addEventListener("resize", close, { signal });
    window.addEventListener("scroll", event => {
        if (event.target instanceof Node && popup.contains(event.target)) return;
        close();
    }, { signal, capture: true });

    if (!popup.matches(":popover-open")) {
        try {
            popup.showPopover();
        } catch (error) {
            if (error?.name !== "InvalidStateError") throw error;
        }
    }

    const rect = anchor.getBoundingClientRect();
    const margin = 8;
    let left = Math.min(rect.left, window.innerWidth - margin - popup.offsetWidth);
    left = Math.max(left, margin);

    let top = rect.bottom + 4;
    if (top + popup.offsetHeight > window.innerHeight - margin) {
        top = Math.max(rect.top - 4 - popup.offsetHeight, margin);
    }

    popup.style.position = "fixed";
    popup.style.inset = "auto";
    popup.style.margin = "0";
    popup.style.top = `${top}px`;
    popup.style.left = `${left}px`;

    (popup.querySelector("input") ?? popup.querySelector("select"))?.focus();
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

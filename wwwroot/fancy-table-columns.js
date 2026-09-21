const instances = new WeakMap();

export function initialize(table, dotNet, options) {
    dispose(table);
    if (!table) return;

    const abort = new AbortController();
    const state = { abort, dotNet, resize: null, reorder: null, frame: 0 };
    instances.set(table, state);

    table.addEventListener("pointerdown", event => onPointerDown(table, state, event), { signal: abort.signal });
    table.addEventListener("keydown", event => onKeyDown(table, state, event), { signal: abort.signal });
    table.addEventListener("click", event => onCellClick(event), { signal: abort.signal });
    refreshResizeAria(table);
    refreshOverflow(table);
}

export function dispose(table) {
    const state = instances.get(table);
    if (!state) return;
    state.abort.abort();
    if (state.frame) cancelAnimationFrame(state.frame);
    instances.delete(table);
}

function onPointerDown(table, state, event) {
    const resizeHandle = event.target.closest("[data-column-resize-handle]");
    if (resizeHandle) {
        event.preventDefault();
        beginResize(table, state, resizeHandle, event);
        return;
    }

    const reorderHandle = event.target.closest("[data-column-reorder-handle]");
    if (reorderHandle) {
        event.preventDefault();
        beginReorder(table, state, reorderHandle, event);
    }
}

function beginResize(table, state, handle, event) {
    const header = handle.closest("th[data-column-key]");
    const headers = [...table.querySelectorAll("thead th[data-column-key]")];
    const columns = [...table.querySelectorAll("col[data-column-key]")];
    const widths = headers.map(item => item.getBoundingClientRect().width);
    const index = headers.indexOf(header);
    if (index < 0) return;

    const minimum = numberValue(header.dataset.minWidth, 24);
    const maximum = numberValue(header.dataset.maxWidth, Number.MAX_SAFE_INTEGER);
    state.resize = { pointerId: event.pointerId, startX: event.clientX, index, widths, columns, minimum, maximum };
    handle.setPointerCapture(event.pointerId);
    handle.classList.add("is-active");

    const move = moveEvent => {
        if (!state.resize || moveEvent.pointerId !== state.resize.pointerId) return;
        const nextWidth = Math.min(maximum, Math.max(minimum, widths[index] + moveEvent.clientX - event.clientX));
        state.resize.widths[index] = nextWidth;
        if (!state.frame) {
            state.frame = requestAnimationFrame(() => {
                state.frame = 0;
                applyWidths(table, columns, state.resize.widths);
            });
        }
    };

    const end = endEvent => {
        if (!state.resize || endEvent.pointerId !== state.resize.pointerId) return;
        handle.classList.remove("is-active");
        handle.removeEventListener("pointermove", move);
        handle.removeEventListener("pointerup", end);
        handle.removeEventListener("pointercancel", end);
        const result = headers.map((item, itemIndex) => ({ key: item.dataset.columnKey, width: state.resize.widths[itemIndex] }));
        state.resize = null;
        state.dotNet.invokeMethodAsync("OnColumnResizeCompleted", result);
    };

    handle.addEventListener("pointermove", move);
    handle.addEventListener("pointerup", end);
    handle.addEventListener("pointercancel", end);
}

function beginReorder(table, state, handle, event) {
    const source = handle.closest("th[data-column-key]");
    state.reorder = { pointerId: event.pointerId, source, target: null, after: false };
    handle.setPointerCapture(event.pointerId);
    source.classList.add("is-dragging");

    const move = moveEvent => {
        if (!state.reorder || moveEvent.pointerId !== state.reorder.pointerId) return;
        clearDropIndicators(table);
        const element = document.elementFromPoint(moveEvent.clientX, moveEvent.clientY);
        const target = element?.closest("th[data-column-key][data-reorderable='true']");
        if (!target || target === source || !table.contains(target)) {
            state.reorder.target = null;
            return;
        }
        const rect = target.getBoundingClientRect();
        const after = moveEvent.clientX >= rect.left + rect.width / 2;
        target.classList.add(after ? "drop-after" : "drop-before");
        state.reorder.target = target;
        state.reorder.after = after;
    };

    const end = endEvent => {
        if (!state.reorder || endEvent.pointerId !== state.reorder.pointerId) return;
        source.classList.remove("is-dragging");
        clearDropIndicators(table);
        handle.removeEventListener("pointermove", move);
        handle.removeEventListener("pointerup", end);
        handle.removeEventListener("pointercancel", end);
        const completed = state.reorder;
        state.reorder = null;
        if (completed.target) {
            state.dotNet.invokeMethodAsync(
                "OnColumnReorderCompleted",
                source.dataset.columnKey,
                completed.target.dataset.columnKey,
                completed.after);
        }
    };

    handle.addEventListener("pointermove", move);
    handle.addEventListener("pointerup", end);
    handle.addEventListener("pointercancel", end);
}

function onKeyDown(table, state, event) {
    const resizeHandle = event.target.closest("[data-column-resize-handle]");
    if (resizeHandle && (event.key === "ArrowLeft" || event.key === "ArrowRight")) {
        event.preventDefault();
        const header = resizeHandle.closest("th[data-column-key]");
        const headers = [...table.querySelectorAll("thead th[data-column-key]")];
        const columns = [...table.querySelectorAll("col[data-column-key]")];
        const widths = headers.map(item => item.getBoundingClientRect().width);
        const index = headers.indexOf(header);
        const minimum = numberValue(header.dataset.minWidth, 24);
        const maximum = numberValue(header.dataset.maxWidth, Number.MAX_SAFE_INTEGER);
        widths[index] = Math.min(maximum, Math.max(minimum, widths[index] + (event.key === "ArrowRight" ? 8 : -8)));
        applyWidths(table, columns, widths);
        state.dotNet.invokeMethodAsync("OnColumnResizeCompleted", headers.map((item, itemIndex) => ({ key: item.dataset.columnKey, width: widths[itemIndex] })));
        return;
    }

    const reorderHandle = event.target.closest("[data-column-reorder-handle]");
    if (reorderHandle && (event.key === "ArrowLeft" || event.key === "ArrowRight")) {
        event.preventDefault();
        const source = reorderHandle.closest("th[data-column-key]");
        const reorderable = [...table.querySelectorAll("thead th[data-column-key][data-reorderable='true']")];
        const index = reorderable.indexOf(source);
        const target = reorderable[index + (event.key === "ArrowRight" ? 1 : -1)];
        if (target) {
            state.dotNet.invokeMethodAsync("OnColumnReorderCompleted", source.dataset.columnKey, target.dataset.columnKey, event.key === "ArrowRight");
        }
    }

    const value = event.target.closest(".fancy-cell-value.is-overflowing");
    if (value && (event.key === "Enter" || event.key === " ")) {
        event.preventDefault();
        toggleCell(value);
    }
}

function onCellClick(event) {
    const value = event.target.closest(".fancy-cell-value.is-overflowing, .fancy-cell-value.is-expanded");
    if (value) toggleCell(value);
}

function toggleCell(value) {
    value.classList.toggle("is-expanded");
    value.setAttribute("aria-expanded", value.classList.contains("is-expanded") ? "true" : "false");
}

function refreshOverflow(table) {
    requestAnimationFrame(() => {
        for (const value of table.querySelectorAll(".fancy-cell-value[data-expandable='true']")) {
            value.classList.remove("is-expanded");
            const overflowing = value.scrollWidth > value.clientWidth + 1;
            value.classList.toggle("is-overflowing", overflowing);
            if (overflowing) {
                value.tabIndex = 0;
                value.setAttribute("role", "button");
                value.setAttribute("aria-expanded", "false");
            } else {
                value.removeAttribute("tabindex");
                value.removeAttribute("role");
                value.removeAttribute("aria-expanded");
            }
        }
    });
}

function applyWidths(table, columns, widths) {
    table.style.tableLayout = "fixed";
    table.style.width = `${widths.reduce((sum, width) => sum + width, 0)}px`;
    columns.forEach((column, index) => {
        const width = `${widths[index]}px`;
        column.style.width = width;
        column.style.minWidth = width;
        column.style.maxWidth = width;
    });
    refreshResizeAria(table);
    refreshOverflow(table);
}

function refreshResizeAria(table) {
    for (const header of table.querySelectorAll("thead th[data-column-key]")) {
        const handle = header.querySelector("[data-column-resize-handle]");
        if (!handle) continue;
        handle.setAttribute("aria-valuenow", Math.round(header.getBoundingClientRect().width).toString());
        handle.setAttribute("aria-valuemin", Math.round(numberValue(header.dataset.minWidth, 24)).toString());
        if (header.dataset.maxWidth) {
            handle.setAttribute("aria-valuemax", Math.round(numberValue(header.dataset.maxWidth, Number.MAX_SAFE_INTEGER)).toString());
        } else {
            handle.removeAttribute("aria-valuemax");
        }
    }
}

function clearDropIndicators(table) {
    for (const header of table.querySelectorAll(".drop-before, .drop-after")) {
        header.classList.remove("drop-before", "drop-after");
    }
}

function numberValue(value, fallback) {
    const number = Number(value);
    return Number.isFinite(number) && number > 0 ? number : fallback;
}

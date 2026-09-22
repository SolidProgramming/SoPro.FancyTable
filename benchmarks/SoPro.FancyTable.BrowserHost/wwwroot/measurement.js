(() => {
    const region = () => document.querySelector("#table-region");
    const matches = expected => {
        const rows = region()?.querySelector("tbody")?.rows;
        if (!rows || rows.length !== expected.count) return false;
        if (rows[0]?.dataset.rowId !== expected.first || rows[rows.length - 1]?.dataset.rowId !== expected.last) return false;
        if (expected.expanded !== null && expected.expanded !== undefined) {
            return (rows[0].querySelector(".tree-toggle-btn .bi-caret-down-fill") !== null) === expected.expanded;
        }
        return true;
    };
    let pending;
    let cancel;
    window.benchmark = {
        matches,
        settle: () => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))),
        arm: ({ selector, event, expected, timeout = 30000 }) => {
            cancel?.();
            performance.clearMarks();
            performance.clearMeasures();
            pending = new Promise(resolve => {
                let start, dom, finished = false, firstFrame, secondFrame;
                const observer = new MutationObserver(check);
                const timer = setTimeout(() => {
                    const rows = region()?.querySelector("tbody")?.rows;
                    finish({ error: `Timed out waiting for expected table DOM/frame opportunity. Expected ${JSON.stringify(expected)}; actual ${JSON.stringify({ count: rows?.length, first: rows?.[0]?.dataset.rowId, last: rows?.[rows.length - 1]?.dataset.rowId, expanded: rows?.[0]?.querySelector('.tree-toggle-btn .bi-caret-down-fill') !== null })}.` });
                }, timeout);
                function finish(result) {
                    if (finished) return;
                    finished = true;
                    clearTimeout(timer);
                    cancelAnimationFrame(firstFrame);
                    cancelAnimationFrame(secondFrame);
                    observer.disconnect();
                    document.removeEventListener(event, onEvent, true);
                    cancel = null;
                    resolve(result);
                }
                function check() {
                    if (start === undefined || dom !== undefined || !matches(expected)) return;
                    dom = performance.now();
                    performance.mark("benchmark-dom");
                    firstFrame = requestAnimationFrame(() => {
                        secondFrame = requestAnimationFrame(() => {
                            if (!matches(expected)) {
                                finish({ error: "Expected DOM changed before frame opportunity." });
                                return;
                            }
                            performance.mark("benchmark-frame-opportunity");
                            performance.measure("benchmark-dom-duration", "benchmark-start", "benchmark-dom");
                            performance.measure("benchmark-frame-duration", "benchmark-start", "benchmark-frame-opportunity");
                            finish({ domMs: dom - start, frameMs: performance.now() - start });
                        });
                    });
                }
                function onEvent(e) {
                    if (start !== undefined || !(e.target instanceof Element) || !e.target.closest(selector)) return;
                    start = performance.now();
                    performance.mark("benchmark-start");
                    check();
                }
                document.addEventListener(event, onEvent, true);
                observer.observe(region(), { childList: true, subtree: true, attributes: true, characterData: true });
                cancel = () => finish({ error: "Measurement cancelled." });
            });
        },
        result: () => pending,
        cancel: () => cancel?.()
    };
})();

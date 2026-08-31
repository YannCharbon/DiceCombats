/*
 * DiceCombats - Copyright (C) 2025 Yann
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

// ===== Utilities =====
function __dc_b64_to_bytes(base64Data) {
    const raw = atob(base64Data || "");
    const out = new Uint8Array(raw.length);
    for (let i = 0; i < raw.length; i++) out[i] = raw.charCodeAt(i);
    return out;
}

function __dc_el(tag, cls, text) {
    const e = document.createElement(tag);
    if (cls) e.className = cls;
    if (text != null) e.textContent = text;
    return e;
}

// One live ResizeObserver per PDF container. Re-rendering a page used to leave its
// observer behind, so repeated calls stacked observers on the same element and a single
// size change fanned out into several redundant renders.
const __dc_pdfObservers = new WeakMap();

// ===== Page renderer (per piece) =====
// Options: { initialScale: 'fit-width' | number }
window.DC_pdfRenderPage = function (containerId, base64Data, pageNumber, opts) {
    const container = document.getElementById(containerId);
    if (!container) return;

    // Rendering clears the container and refills it asynchronously, which makes the page
    // flash and resets the reader's zoom. Never redo it for content already shown.
    const token = pageNumber + ":" + ((base64Data && base64Data.length) || 0);
    if (container.dataset.dcPdfToken === token && container.childElementCount > 0) return;
    container.dataset.dcPdfToken = token;

    // Reset any transform leftovers that could mirror content
    container.style.transform = "none";

    opts = opts || {};
    const bytes = __dc_b64_to_bytes(base64Data);

    const start = () => {
        if (!window.pdfjsLib) { setTimeout(start, 60); return; }

        const state = {
            uiScale: 1.2,   // UI scale (CSS)
            dpiScale: Math.max(1, window.devicePixelRatio || 1), // render scale multiplier
            pdf: null,
            page: null,
            viewportAt1: null,
            lastUiScale: null
        };

        const loadingTask = window.pdfjsLib.getDocument({ data: bytes });
        loadingTask.promise.then(pdf => {
            state.pdf = pdf;
            pdf.getPage(pageNumber).then(page => {
                state.page = page;
                state.viewportAt1 = page.getViewport({ scale: 1.0 });

                // UI
                const wrap = __dc_el("div", "pdf-page");
                const toolbar = __dc_el("div", "pdf-toolbar");
                const left = __dc_el("div", "pdf-toolbar-left");
                const right = __dc_el("div", "pdf-toolbar-right");

                const btnFit = __dc_el("button", "pdf-btn", "Fit");
                const btn100 = __dc_el("button", "pdf-btn", "100%");
                const btnMinus = __dc_el("button", "pdf-btn", "−");
                const btnPlus = __dc_el("button", "pdf-btn", "+");
                const info = __dc_el("span", "pdf-zoom-info", "");

                left.appendChild(__dc_el("div", "pdf-page-header", "Page " + pageNumber));
                right.append(btnFit, btn100, btnMinus, btnPlus, info);
                toolbar.append(left, right);

                const canvas = __dc_el("canvas", "dc-pdf-canvas");
                canvas.style.transform = "none"; // avoid inherited mirror
                canvas.style.imageRendering = "auto";

                wrap.append(toolbar, canvas);
                container.innerHTML = "";
                container.appendChild(wrap);

                //// Wheel zoom (Ctrl+wheel or just wheel)
                //wrap.addEventListener("wheel", (ev) => {
                //    if (!ev.deltaY) return;
                //    const factor = ev.deltaY > 0 ? 1 / 1.1 : 1.1;
                //    state.uiScale = Math.max(0.25, Math.min(6.0, state.uiScale * factor));
                //    ev.preventDefault();
                //    render();
                //}, { passive: false });

                btnFit.onclick = () => { state.uiScale = "fit-width"; render(true); };
                btn100.onclick = () => { state.uiScale = 1.0; render(true); };
                btnMinus.onclick = () => { state.uiScale = Math.max(0.25, (typeof state.uiScale === "number" ? state.uiScale : 1.0) / 1.15); render(true); };
                btnPlus.onclick = () => { state.uiScale = Math.min(6.0, (typeof state.uiScale === "number" ? state.uiScale : 1.0) * 1.15); render(true); };

                // Initial scale
                state.uiScale = (opts.initialScale === "fit-width") ? "fit-width" :
                    (typeof opts.initialScale === "number" ? opts.initialScale : 1.2);

                function computeUiScale() {
                    if (state.uiScale === "fit-width") {
                        const styles = getComputedStyle(container);
                        const pad = parseFloat(styles.paddingLeft) + parseFloat(styles.paddingRight);
                        const usable = container.clientWidth - pad;
                        // While the popup is still hidden the container measures 0; keep the
                        // last good scale instead of snapping to the minimum and back.
                        if (!(usable > 0)) return state.lastUiScale || 1.2;
                        const s = usable / state.viewportAt1.width;
                        return Math.max(0.5, Math.min(4.0, s));
                    }
                    return state.uiScale || 1.2;
                }

                function render(force) {
                    const ui = computeUiScale();
                    // render() resizes the canvas inside the observed subtree; without this
                    // guard every relayout bounced the page between two sizes.
                    if (!force && state.lastUiScale !== null && Math.abs(ui - state.lastUiScale) < 0.005) {
                        return;
                    }
                    state.lastUiScale = ui;
                    const renderScale = ui * state.dpiScale;

                    const vp = state.page.getViewport({ scale: renderScale });

                    // Physical canvas size in device pixels (no ctx.setTransform, avoids flip bugs)
                    canvas.width = Math.floor(vp.width);
                    canvas.height = Math.floor(vp.height);

                    // CSS size in CSS pixels (ui scale only)
                    canvas.style.width = Math.floor(vp.width / state.dpiScale) + "px";
                    canvas.style.height = Math.floor(vp.height / state.dpiScale) + "px";

                    info.textContent = Math.round(ui * 100) + "%";

                    const ctx = canvas.getContext("2d", { alpha: false });
                    ctx.save();
                    // Ensure we don't inherit any transforms
                    ctx.setTransform(1, 0, 0, 1, 0, 0);
                    ctx.clearRect(0, 0, canvas.width, canvas.height);

                    // Render at high resolution directly (no setTransform for DPR)
                    state.page.render({ canvasContext: ctx, viewport: vp }).promise.then(() => {
                        ctx.restore();
                    });
                }

                const previousObserver = __dc_pdfObservers.get(container);
                if (previousObserver) { previousObserver.disconnect(); }

                let relayoutPending = false;
                const ro = new ResizeObserver(() => {
                    if (state.uiScale !== "fit-width" || relayoutPending) return;
                    relayoutPending = true;
                    requestAnimationFrame(() => { relayoutPending = false; render(false); });
                });
                // Observe the parent: its width is what drives the fit, and unlike the
                // container it is not resized by render() itself.
                ro.observe(container.parentElement || container);
                __dc_pdfObservers.set(container, ro);

                render(true);
            });
        }, reason => {
            container.innerHTML = '<div style="color:#f55">Failed to load PDF: ' +
                (reason && reason.message ? reason.message : reason) + '</div>';
        });
    };

    start();
};

// Basic styles for toolbar
(function injectCssOnce() {
    if (document.getElementById("__dc_pdf_css")) return;
    const css = `
  .pdf-page { position: relative; }
  .pdf-toolbar {
    display:flex; justify-content:space-between; align-items:center;
    padding:6px 8px; margin-bottom:6px;
    border:1px solid #666; border-radius:6px; background:#222; color:#ddd;
  }
  .pdf-toolbar .pdf-btn {
    margin-left:6px; border:1px solid #555; border-radius:4px; background:#333; color:#eee;
    padding:2px 8px; cursor:pointer;
  }
  .pdf-toolbar .pdf-btn:hover { background:#3b3b3b; }
  .pdf-page-header { font-weight:600; }
  .pdf-zoom-info { margin-left:8px; opacity:.8; }
  .dc-pdf-canvas { display:block; max-width:100%; height:auto; }
  `;
    const el = document.createElement("style");
    el.id = "__dc_pdf_css";
    el.textContent = css;
    document.head.appendChild(el);
})();

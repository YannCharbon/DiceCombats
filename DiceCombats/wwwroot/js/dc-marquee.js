/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * Roster rows that do not fit drift sideways and back, the way a bus
 * destination sign cycles through its stops. A scrollbar per card would be
 * noise in a grid of them, and CSS alone cannot tell whether a row overflows,
 * so the measuring happens here: the class and the two custom properties this
 * sets are what app.css animates.
 *
 * Nothing calls in from Blazor. Rows are found as they appear, so a re-render,
 * a window resize or a font swap all settle by themselves.
 */
(function () {
    'use strict';

    var SPEED = 30;   // px travelled per second
    var HOLD = 1.5;   // seconds held still at each end
    var watched = new WeakMap();

    function measure(row) {
        var track = row.firstElementChild;
        if (!track) return;

        // Transforms do not affect layout, so writing these back cannot
        // retrigger the observer that called us.
        var overflow = track.scrollWidth - row.clientWidth;

        if (overflow > 1) {
            var travel = overflow + 2;   // a hair past the end, so the last
                                         // chip clears the edge
            var seconds = travel / SPEED + HOLD * 2;
            row.style.setProperty('--dc-marquee-shift', -travel + 'px');
            row.style.setProperty('--dc-marquee-time', seconds.toFixed(2) + 's');
            row.classList.add('dc-roster-marquee');
        } else {
            row.classList.remove('dc-roster-marquee');
            row.style.removeProperty('--dc-marquee-shift');
            row.style.removeProperty('--dc-marquee-time');
        }
    }

    function watch(row) {
        if (watched.has(row)) {
            measure(row);
            return;
        }

        if (typeof ResizeObserver === 'undefined') {
            measure(row);
            watched.set(row, null);
            return;
        }

        var observer = new ResizeObserver(function () { measure(row); });
        observer.observe(row);
        if (row.firstElementChild) observer.observe(row.firstElementChild);
        watched.set(row, observer);
        measure(row);
    }

    function scan() {
        var rows = document.querySelectorAll('.dc-roster');
        for (var i = 0; i < rows.length; i++) watch(rows[i]);
    }

    // One pass per frame at most: Blazor can touch the tree many times while
    // a page renders.
    var queued = false;
    function schedule() {
        if (queued) return;
        queued = true;
        requestAnimationFrame(function () {
            queued = false;
            scan();
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', scan);
    } else {
        scan();
    }

    if (typeof MutationObserver !== 'undefined') {
        new MutationObserver(schedule).observe(document.documentElement, {
            childList: true,
            subtree: true
        });
    }
})();

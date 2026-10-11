/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 *
 * The Text custom field is sized by what is in it.
 *
 * A fixed-width box on a combat strip is wrong in both directions: it wastes a
 * third of the row when the note is "prone", and it hides the end of the note
 * when it is a sentence. So the box grows with its content, and there are two
 * levels of wrapping, in this order:
 *
 *   1. the whole field group widens until it no longer fits the strip's row,
 *      at which point flex-wrap drops it onto a row of its own;
 *   2. there, max-width clamps it to the full width and the box wraps its own
 *      lines, growing in height instead.
 *
 * CSS cannot measure text, so the width is computed here and handed back as
 * --dc-grow-w on the .dc-fgrp-grow cell; app.css does the rest.
 *
 * Nothing calls in from Blazor. Boxes are found as they appear, so a
 * re-render, a window resize or a font swap all settle by themselves.
 */
(function () {
    'use strict';

    var MIN_W = 150;   // px, never narrower than this
    var PAD = 20;      // the box's own horizontal padding + border
    var GAP = 24;      // the group's padding + the gap after its label

    var sizer = null;

    function getSizer() {
        if (sizer && sizer.isConnected) return sizer;
        sizer = document.createElement('span');
        sizer.setAttribute('aria-hidden', 'true');
        sizer.style.cssText =
            'position:absolute;left:-9999px;top:0;white-space:pre;visibility:hidden;' +
            'padding:0;margin:0;border:0';
        document.body.appendChild(sizer);
        return sizer;
    }

    function fit(ta) {
        var group = ta.closest('.dc-fgrp-grow');
        if (!group) return;

        var cs = window.getComputedStyle(ta);
        var s = getSizer();
        s.style.font = cs.font;
        s.style.letterSpacing = cs.letterSpacing;

        // The widest single line decides the width. Splitting on the newline
        // matters: a two-line note should not ask for the width of both lines.
        var text = ta.value || ta.placeholder || '';
        var lines = text.split(String.fromCharCode(10));
        var widest = 0;
        for (var i = 0; i < lines.length; i++) {
            s.textContent = lines[i] || ' ';
            if (s.offsetWidth > widest) widest = s.offsetWidth;
        }

        var label = group.querySelector('.dc-flabel');
        var want = widest + PAD + GAP + (label ? label.offsetWidth : 0);
        group.style.setProperty('--dc-grow-w', Math.max(MIN_W, want) + 'px');

        // scrollHeight excludes the border and the box is border-box, hence +2.
        ta.style.height = 'auto';
        ta.style.height = Math.max(32, ta.scrollHeight + 2) + 'px';
    }

    function fitAll() {
        var boxes = document.querySelectorAll('textarea[data-dc-grow]');
        for (var i = 0; i < boxes.length; i++) fit(boxes[i]);
    }

    var pending = false;
    function schedule() {
        if (pending) return;
        pending = true;
        window.requestAnimationFrame(function () {
            pending = false;
            fitAll();
        });
    }

    // Typing resizes only the box being typed in; everything else is a batch.
    document.addEventListener('input', function (e) {
        var t = e.target;
        if (t && t.matches && t.matches('textarea[data-dc-grow]')) fit(t);
    }, true);

    window.addEventListener('resize', schedule);

    // Blazor rewrites the DOM under us on every render, and a value set from
    // C# fires no input event, so a coalesced pass after each mutation burst is
    // what keeps a re-rendered strip correctly sized.
    if (window.MutationObserver) {
        new MutationObserver(schedule).observe(document.documentElement, {
            childList: true,
            subtree: true
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', schedule);
    } else {
        schedule();
    }
})();

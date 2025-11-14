/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using DiceCombats.Rendering.Contracts;
using DiceCombats.Rendering.Util;

namespace DiceCombats.Rendering.Renderers
{
    public sealed class TextRenderer : IRenderer
    {
        public bool CanRender(IContentItem item)
            => item.Kind.Equals("text", StringComparison.OrdinalIgnoreCase);

        public IEnumerable<RenderPiece> RenderMany(IContentItem item)
        {
            var text = item.Text ?? string.Empty;

            RenderFragment frag = b =>
            {
                b.AddMarkupContent(0,
                    $"<div style=\"white-space:pre-wrap\">{System.Net.WebUtility.HtmlEncode(text)}</div>");
            };

            var index = new SimpleIndex(new[] { ("self", text) });
            yield return new RenderPiece(frag, index, item.Name);
        }
    }
}

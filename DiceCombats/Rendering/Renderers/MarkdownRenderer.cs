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
using Markdig;

namespace DiceCombats.Rendering.Renderers
{
    public sealed class MarkdownRenderer : IRenderer
    {
        private static readonly MarkdownPipeline _pipeline =
            new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        public bool CanRender(IContentItem item)
            => item.Kind.Equals("markdown", StringComparison.OrdinalIgnoreCase);

        public IEnumerable<RenderPiece> RenderMany(IContentItem item)
        {
            var md = item.Text ?? string.Empty;
            var html = Markdig.Markdown.ToHtml(md, _pipeline);

            RenderFragment frag = b => b.AddMarkupContent(0, html);
            var index = new SimpleIndex(new[] { ("self", md) });

            yield return new RenderPiece(frag, index, item.Name);
        }
    }
}

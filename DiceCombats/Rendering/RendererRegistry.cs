/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Collections.Generic;
using System.Linq;
using DiceCombats.Rendering.Contracts;

namespace DiceCombats.Rendering
{
    public sealed class RendererRegistry
    {
        private readonly List<IRenderer> _renderers = new();

        public RendererRegistry(IEnumerable<IRenderer> renderers)
        {
            if (renderers != null)
                _renderers.AddRange(renderers);
        }

        public IEnumerable<RenderPiece> RenderMany(IContentItem item)
        {
            var r = _renderers.FirstOrDefault(x => x.CanRender(item));
            if (r is null) return Enumerable.Empty<RenderPiece>();
            var pieces = r.RenderMany(item);
            return pieces ?? Enumerable.Empty<RenderPiece>();
        }
    }
}

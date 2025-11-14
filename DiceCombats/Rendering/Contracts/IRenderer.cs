/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace DiceCombats.Rendering.Contracts
{
    /// <summary>
    /// One atomic, searchable UI unit produced by a renderer
    /// (e.g., one HTML card extracted from a page, or one PDF page).
    /// </summary>
    public sealed record RenderPiece(RenderFragment UI, IIndexedText Index, string? Key = null);

    public interface IRenderer
    {
        bool CanRender(IContentItem item);

        /// <summary>
        /// Produce one or more pieces (UI + searchable text) from the content item.
        /// </summary>
        IEnumerable<RenderPiece> RenderMany(IContentItem item);
    }
}

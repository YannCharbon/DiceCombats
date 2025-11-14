/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Collections.Generic;

namespace DiceCombats.Rendering.Contracts
{
    /// <summary>
    /// Minimal searchable text abstraction for a rendered piece.
    /// Implementations should return one or more (id,text) pairs.
    /// </summary>
    public interface IIndexedText
    {
        IEnumerable<(string id, string text)> EnumerateDocuments();
    }
}

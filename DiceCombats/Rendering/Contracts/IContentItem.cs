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
    public interface IContentItem
    {
        string Kind { get; }
        string? Name { get; }
        byte[]? Data { get; }
        string? Text { get; }
        string? Path { get; }

        // Read-only view on meta settings (simple string dictionary)
        IReadOnlyDictionary<string, string> Meta { get; }
    }
}

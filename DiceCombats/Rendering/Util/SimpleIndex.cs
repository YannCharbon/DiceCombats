/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Collections.Generic;
using DiceCombats.Rendering.Contracts;

namespace DiceCombats.Rendering.Util
{
    /// <summary>
    /// Lightweight IIndexedText implementation over a fixed set of (id,text) pairs.
    /// </summary>
    public sealed class SimpleIndex : IIndexedText
    {
        private readonly IReadOnlyList<(string id, string text)> _docs;

        public SimpleIndex(IEnumerable<(string id, string text)> docs)
        {
            if (docs is IReadOnlyList<(string id, string text)> list)
                _docs = list;
            else
                _docs = new List<(string id, string text)>(docs);
        }

        public IEnumerable<(string id, string text)> EnumerateDocuments() => _docs;
    }
}

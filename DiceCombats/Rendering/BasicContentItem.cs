/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Collections.Generic;
using DiceCombats.Rendering.Contracts;

namespace DiceCombats.Rendering
{
    public sealed class BasicContentItem : IContentItem
    {
        public string Kind { get; }
        public string? Name { get; }
        public byte[]? Data { get; }
        public string? Text { get; }
        public string? Path { get; }
        public IReadOnlyDictionary<string, string> Meta { get; }

        public BasicContentItem(
            string kind,
            string? name,
            byte[]? data,
            string? text,
            string? path,
            IReadOnlyDictionary<string, string> meta)
        {
            Kind = kind;
            Name = name;
            Data = data;
            Text = text;
            Path = path;
            Meta = meta;
        }
    }
}

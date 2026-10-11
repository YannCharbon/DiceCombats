/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace DiceCombats
{
    /// <summary>
    /// How a detailed-popup field presents its entries when it opens.
    /// Serialised by name, so adding a mode later cannot renumber the existing ones.
    /// List is deliberately the zero value: it is what every creature saved before this
    /// enum existed was using.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DCDetailedPopupDisplayMode
    {
        List = 0,
        Grid = 1,
        Tabs = 2
    }

    public class DCCreatureDetailedPopupField : DCCreatureCustomField
    {
        public override string FieldType => "DetailedPopup";
        public override string Discriminator => nameof(DCCreatureDetailedPopupField);

        public class ContentEntry
        {
            public string Name { get; set; } = string.Empty;

            // "text", "markdown", "html", "pdf", "image" (future)
            public string Kind { get; set; } = "text";

            // Inline content. For "text"/"markdown"/"html" this is the main source.
            public string? Inline { get; set; }

            // Optional file path (if user picked a file). Used for "pdf" or large html.
            public string? FilePath { get; set; }

            // Optional blob content (loaded from FilePath or elsewhere).
            public byte[]? Blob { get; set; }

            public bool ReadOnly { get; set; } = false;

            // Maximum visual height for renderers that can constrain height (0 = auto).
            public int MaxHeight { get; set; } = 0;

            // Simple and robust metadata bag (string->string) to configure renderers.
            // Typical keys for HTML template renderer:
            //   "selector": ".someClass"
            //   "template": "<div class='card'>{{title}}...</div>"
            //   "css": ".card{...}"
            //   "map.json": "{\"title\":\"h1\",\"field1\":\".myField1\"}"
            public Dictionary<string, string> Meta { get; set; } = new();
        }

        public List<ContentEntry> Contents { get; set; } = new List<ContentEntry>();

        /// <summary>
        /// Legacy storage for the presentation mode: false = list, true = grid. Kept as a
        /// real stored property so that files written by this version still open in older
        /// builds, and so that files written by older builds still land on the right mode
        /// here. <see cref="DisplayMode"/> is the authoritative one.
        /// </summary>
        public bool RenderModeGridEnabled { get; set; } = false;

        private DCDetailedPopupDisplayMode? _displayMode;

        /// <summary>
        /// How the entries are presented when the popup opens: every entry stacked, every
        /// entry side by side, or one at a time behind tabs.
        ///
        /// The backing field is nullable on purpose. When it is null — which is every
        /// creature saved before this property existed — the getter falls back to the
        /// legacy <see cref="RenderModeGridEnabled"/> boolean, so an old creature opens in
        /// exactly the mode it was saved in. Because the fallback lives in the getter
        /// rather than in a migration step, it does not matter which of the two properties
        /// System.Text.Json happens to apply first.
        /// </summary>
        public DCDetailedPopupDisplayMode DisplayMode
        {
            get => _displayMode ?? (RenderModeGridEnabled
                ? DCDetailedPopupDisplayMode.Grid
                : DCDetailedPopupDisplayMode.List);
            set
            {
                _displayMode = value;
                // keep the legacy flag in step so a downgrade still reads sensibly
                RenderModeGridEnabled = value == DCDetailedPopupDisplayMode.Grid;
            }
        }

        public DCCreatureDetailedPopupField()
        {
            Debug.WriteLine("DCCreatureDetailedPopupField");
        }

        public override object GetValue() => Contents;

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureDetailedPopupField
            {
                Title = this.Title,
                Contents = this.Contents, // shallow copy (OK for current usage)
                DisplayMode = this.DisplayMode,
                // Every other field type copies this; omitting it here meant a shared
                // detailed-popup field stopped being shared as soon as DCCombat cloned
                // it per instance.
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }

        public void AddNewContent()
        {
            Contents.Add(new ContentEntry { });
        }
    }
}

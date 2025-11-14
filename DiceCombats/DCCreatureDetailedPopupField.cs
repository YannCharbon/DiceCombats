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

namespace DiceCombats
{
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

        public bool RenderModeGridEnabled { get; set; } = false;

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
                RenderModeGridEnabled = this.RenderModeGridEnabled
            };
        }

        public void AddNewContent()
        {
            Contents.Add(new ContentEntry { });
        }
    }
}

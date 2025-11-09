/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using BootstrapBlazor.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Markdig;
using MudBlazor;

namespace DiceCombats
{
    public class DCCreatureDetailedPopupField : DCCreatureCustomField
    {
        public override string FieldType => "DetailedPopup";
        public override string Discriminator => nameof(DCCreatureDetailedPopupField);

        public class SubField
        {
            public string Name { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public bool IsEditable { get; set; } = true;
            public bool IsMarkdown { get; set; } = false;
            public int MaxHeight { get; set; } = 0;

            public string GetMarkdownContentAsHtml()
            {
                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions() // This includes PipeTables and more
                    .Build();

                return Markdown.ToHtml(Content, pipeline);
            }
        }

        public List<SubField> SubFields { get; set; } = new List<SubField>();
        public bool RenderModeGridEnabled { get; set; } = false;

        public DCCreatureDetailedPopupField()
        {
            Debug.WriteLine("DCCreatureDetailedPopupField");
        }

        public override object GetValue()
        {
            return SubFields;
        }

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureDetailedPopupField
            {
                Title = this.Title,
                SubFields = this.SubFields,
                RenderModeGridEnabled = this.RenderModeGridEnabled,
            };
        }

        public void AddNewField()
        {
            SubFields.Add(new SubField { });
        }
    }
}

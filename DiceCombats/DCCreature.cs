﻿/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MudBlazor;
using Markdig;

namespace DiceCombats
{
    public class DCCreature : ICloneable
    {
        public DCJsonMitigatorId JsonMitigatorId { get; set; } = DCJsonMitigatorId.CREATURE;
        public Guid Id { get; set; } = Guid.Empty;
        public string Name { get; private set; } = string.Empty;

        [JsonInclude]
        public string ImageSheetBas64 { get; private set; } = string.Empty;
        [JsonInclude]
        public string HtmlSheet { get; private set; } = string.Empty;
        [JsonInclude]
        public string MarkdownSheet { get; private set; } = string.Empty;

        public List<DCCreatureCustomField> CustomFields { get; set; } = new List<DCCreatureCustomField>();
        public uint InCombatInstanceCount { get; set; } = 1;
        public bool IsPlayer { get; set; } = false;
        public bool IsManual { get; set; } = false;
        public int InitiativeRank { get; set; } = 0;
        public int InitiativeRoll { get; set; } = 0;

        public DCCreature(string name)
        {
            Name = name;
            Id = Guid.NewGuid();
        }

        public void SetImageSheetBase64(string imageSheetBase64)
        {
            ImageSheetBas64 = imageSheetBase64;
        }

        public void SetHtmlSheet(string htmlSheet)
        {
            HtmlSheet = htmlSheet;
        }

        public void SetMarkdownSheet(string markdownSheet)
        {
            MarkdownSheet = markdownSheet;
        }

        public string GetMarkdownSheetAsHtml()
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions() // This includes PipeTables and more
                .Build();

            return Markdown.ToHtml(MarkdownSheet, pipeline);
        }

        public string SerializeToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public object Clone()
        {
            var json = JsonSerializer.Serialize(this);
            return JsonSerializer.Deserialize<DCCreature>(json)!;
        }
    }
}

/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using DiceCombats.Automations;

namespace DiceCombats.Portability;

/// <summary>
/// "The same content" for an import (workplan §1.2, spec §4.4): a canonical
/// JSON string of what describes an asset, in a fixed order. Play data (hit
/// points, ticked boxes, conditions, initiative, instances) is left out, so
/// a creature that only took damage is still Up to date. Pure functions.
/// </summary>
public static class DCFingerprint
{
    public static string Of(DCCreature creature) => new JsonObject
    {
        ["Name"] = creature.Name,
        ["ImageSheetBas64"] = creature.ImageSheetBas64,
        ["HtmlSheet"] = creature.HtmlSheet,
        ["MarkdownSheet"] = creature.MarkdownSheet,
        ["IsPlayer"] = creature.IsPlayer,
        ["IsManual"] = creature.IsManual,
        ["OpenLinksInApp"] = creature.OpenLinksInApp,
        ["CustomFields"] = new JsonArray(creature.CustomFields.Select(f => (JsonNode?)Field(f)).ToArray()),
    }.ToJsonString();

    /// <summary>A custom field, as in a creature or as a user custom field (template).</summary>
    public static string Of(DCCreatureCustomField field) => Field(field).ToJsonString();

    public static string Of(DCCombat combat) => new JsonObject
    {
        ["Name"] = combat.Name,
        ["Campaign"] = combat.Campaign,
        ["Notes"] = combat.Notes,
        ["CreaturesList"] = new JsonArray(combat.CreaturesList
            .Select(e => (JsonNode?)new JsonObject { ["Id"] = e.Id.ToString("D"), ["InCombatInstanceCount"] = e.InCombatInstanceCount })
            .ToArray()),
    }.ToJsonString();

    public static string Of(DCPlayerGroup group) => new JsonObject
    {
        ["Name"] = group.Name,
        ["PlayerIds"] = Strings(group.PlayerIds.Select(id => id.ToString("D"))),
    }.ToJsonString();

    public static string Of(ExternalAppDefinition app) => new JsonObject
    {
        ["Name"] = app.Name,
        ["BaseUrl"] = app.BaseUrl,
        ["IsEnabled"] = app.IsEnabled,
        ["AuthType"] = app.AuthType,
        ["DefaultHeaders"] = Headers(app.DefaultHeaders),
    }.ToJsonString();

    public static string Of(EventActionBinding binding) => new JsonObject
    {
        ["Name"] = binding.Name,
        ["IsEnabled"] = binding.IsEnabled,
        ["Trigger"] = binding.Trigger == null ? null : JsonSerializer.SerializeToNode(binding.Trigger),
        ["Condition"] = binding.Condition == null ? null : JsonSerializer.SerializeToNode(binding.Condition),
        ["CooldownMs"] = binding.CooldownMs,
        ["Actions"] = new JsonArray(binding.Actions.Select(a => (JsonNode?)Action(a)).ToArray()),
    }.ToJsonString();

    private static JsonObject Action(ActionDefinition action)
    {
        var node = new JsonObject
        {
            ["Name"] = action.Name,
            ["Type"] = action.Type,
            ["ContinueOnFailure"] = action.ContinueOnFailure,
        };
        switch (action)
        {
            case HttpRequestActionDefinition http:
                node["ExternalAppId"] = http.ExternalAppId?.ToString("D");
                node["Method"] = http.Method;
                node["Url"] = http.Url;
                node["Headers"] = Headers(http.Headers);
                node["BodyTemplate"] = http.BodyTemplate;
                node["TimeoutMs"] = http.TimeoutMs;
                break;
            case DelayActionDefinition delay:
                node["Milliseconds"] = delay.Milliseconds;
                break;
        }
        return node;
    }

    // A set of headers, sorted by name; the value of a header that may carry
    // a secret is left out, since exports blank it.
    private static JsonObject Headers(Dictionary<string, string> headers)
    {
        var node = new JsonObject();
        foreach (var (name, value) in headers.OrderBy(h => h.Key, StringComparer.Ordinal))
        {
            node[name] = DCBundleCollector.IsSecretHeader(name) ? null : value;
        }
        return node;
    }

    private static JsonObject Field(DCCreatureCustomField field)
    {
        var node = new JsonObject
        {
            ["Discriminator"] = field.Discriminator,
            ["Title"] = field.Title,
            ["SharedAcrossCreatureInstances"] = field.SharedAcrossCreatureInstances,
        };
        switch (field)
        {
            case DCCreatureHitPointsField hp:
                node["MaximumHP"] = hp.MaximumHP;
                node["DamageKinds"] = new JsonArray(hp.DamageKinds.Select(k => (JsonNode?)new JsonObject
                {
                    ["Name"] = k.Name, ["Color"] = k.Color, ["Icon"] = k.Icon,
                }).ToArray());
                node["Tags"] = Strings(hp.Tags);
                node["MitigationRules"] = new JsonArray(hp.MitigationRules.Select(r => (JsonNode?)new JsonObject
                {
                    ["Name"] = r.Name, ["Factor"] = r.Factor, ["FlatReduction"] = r.FlatReduction, ["Rounding"] = r.Rounding,
                }).ToArray());
                node["Mitigations"] = new JsonArray(hp.Mitigations.Select(m => (JsonNode?)new JsonObject
                {
                    ["Kind"] = m.Kind, ["RuleName"] = m.RuleName, ["RequiredTag"] = m.RequiredTag, ["TagMustBePresent"] = m.TagMustBePresent,
                }).ToArray());
                node["AllowNegative"] = hp.AllowNegative;
                break;
            case DCCreatureNumericField numeric:
                node["Value"] = numeric.Value;
                node["MaxValue"] = numeric.MaxValue;
                node["ShowMaxValue"] = numeric.ShowMaxValue;
                node["Unit"] = numeric.Unit;
                node["ShowUnit"] = numeric.ShowUnit;
                break;
            case DCCreatureCheckboxField checkbox:
                node["Labels"] = Strings(checkbox.Labels);
                node["TextFieldsLabels"] = Strings(checkbox.TextFieldsLabels);
                node["ShowFieldName"] = checkbox.ShowFieldName;
                node["ShowTextFields"] = checkbox.ShowTextFields;
                node["HighlightWhenChecked"] = checkbox.HighlightWhenChecked;
                break;
            case DCCreatureCheckboxGridField grid:
                node["Rows"] = grid.Rows;
                node["Columns"] = grid.Columns;
                node["HasRowHeaders"] = grid.HasRowHeaders;
                node["HasColumnHeaders"] = grid.HasColumnHeaders;
                node["RowHeaders"] = Strings(grid.RowHeaders);
                node["ColumnHeaders"] = Strings(grid.ColumnHeaders);
                break;
            case DCCreatureConditionsField conditions:
                node["Labels"] = Strings(conditions.Labels);
                break;
            case DCCreatureColorField color:
                node["Value"] = color.Value;
                break;
            case DCCreatureStatsField stats:
                node["Count"] = stats.Count;
                node["StatsLabels"] = Strings(stats.StatsLabels);
                node["StatsValues"] = new JsonArray(stats.StatsValues.Select(v => (JsonNode?)v).ToArray());
                node["ValuesAreEditable"] = stats.ValuesAreEditable;
                break;
            case DCCreatureTextField text:
                node["Text"] = text.Text;
                break;
            case DCCreatureTextGridField textGrid:
                node["Rows"] = textGrid.Rows;
                node["Columns"] = textGrid.Columns;
                node["HasRowHeaders"] = textGrid.HasRowHeaders;
                node["HasColumnHeaders"] = textGrid.HasColumnHeaders;
                node["RowHeaders"] = Strings(textGrid.RowHeaders);
                node["ColumnHeaders"] = Strings(textGrid.ColumnHeaders);
                node["HasTextLabels"] = new JsonArray(textGrid.HasTextLabels.Select(r => (JsonNode?)new JsonArray(r.Select(b => (JsonNode?)b).ToArray())).ToArray());
                node["TextLabels"] = new JsonArray(textGrid.TextLabels.Select(r => (JsonNode?)Strings(r)).ToArray());
                node["TextValues"] = new JsonArray(textGrid.TextValues.Select(r => (JsonNode?)Strings(r)).ToArray());
                break;
            case DCCreatureDetailedPopupField popup:
                node["DisplayMode"] = popup.DisplayMode.ToString();
                node["Contents"] = new JsonArray(popup.Contents.Select(c => (JsonNode?)new JsonObject
                {
                    ["Name"] = c.Name,
                    ["Kind"] = c.Kind,
                    ["Inline"] = c.Inline,
                    ["Blob"] = c.Blob == null ? null : Convert.ToHexStringLower(SHA256.HashData(c.Blob)),
                    ["ReadOnly"] = c.ReadOnly,
                    ["MaxHeight"] = c.MaxHeight,
                    ["Meta"] = MetaSet(c.Meta),
                }).ToArray());
                break;
        }
        return node;
    }

    private static JsonArray MetaSet(Dictionary<string, string> meta) =>
        new(meta.Select(m => m.Key + "=" + m.Value).OrderBy(s => s, StringComparer.Ordinal).Select(s => (JsonNode?)s).ToArray());

    private static JsonArray Strings(IEnumerable<string?> values) => new(values.Select(v => (JsonNode?)v).ToArray());
}

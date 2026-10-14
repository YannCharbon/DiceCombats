/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DiceCombats.Automations;

namespace DiceCombats.Portability;

/// <summary>
/// A DiceCombats export file (spec §2.2): what the user chose (Roots) and
/// everything it needs, in one JSON file.
/// </summary>
public sealed class DCBundle
{
    public const string FormatName = "DiceCombats.Bundle";
    public const string OtherAppFormatName = "DiceAudio.Bundle";
    public const int CurrentVersion = 1;
    public const long MaxSizeBytes = 200L * 1024 * 1024;

    public string Format { get; set; } = FormatName;
    public int FormatVersion { get; set; } = CurrentVersion;
    public string AppVersion { get; set; } = "";
    public Guid BundleId { get; set; } = Guid.NewGuid();
    public string ExportedAtUtc { get; set; } = DateTime.UtcNow.ToString("o");
    public List<BundleRoot> Roots { get; set; } = new();
    public List<DCCreature> Creatures { get; set; } = new();
    public List<DCCombat> Combats { get; set; } = new();
    public List<DCPlayerGroup> PlayerGroups { get; set; } = new();
    public List<DCCreatureCustomField> FieldTemplates { get; set; } = new();
    public List<ExternalAppDefinition> ExternalApps { get; set; } = new();
    public List<EventActionBinding> Automations { get; set; } = new();
    public List<BundleWarning> Warnings { get; set; } = new();
}

public static class DCBundleKinds
{
    public const string Creature = "Creature";
    public const string Combat = "Combat";
    public const string PlayerGroup = "PlayerGroup";
    public const string FieldTemplate = "FieldTemplate";
    public const string ExternalApp = "ExternalApp";
    public const string Automation = "Automation";
    public const string Library = "Library";
}

public static class DCBundleWarningCodes
{
    public const string CreatureNotInLibrary = "CreatureNotInLibrary";
    public const string TokenRemoved = "TokenRemoved";
    /// <summary>An HTTP request body that mentions a token, key or secret, exported as it is.</summary>
    public const string BodyKeptAsIs = "BodyKeptAsIs";
    public const string LegacyFormat = "LegacyFormat";
    public const string BrokenReferences = "BrokenReferences";
}

public enum DCBundleReadError { UnreadableFile, OtherAppFile, NewerFormat, FileTooLarge }

/// <summary>A file that cannot be imported; nothing has been changed. Each error has its text in Portability.resx.</summary>
public sealed class DCBundleReadException : Exception
{
    public DCBundleReadException(DCBundleReadError error, string message, Exception? inner = null) : base(message, inner)
    {
        Error = error;
    }

    public DCBundleReadError Error { get; }
}

/// <summary>
/// Writes a bundle, and reads any DiceCombats file: a bundle, or an export
/// from before bundles (one creature, one combat, a list of either), which is
/// converted on the fly (spec §2.2, legacy files). Validation is pipeline
/// step 1 of spec §2.1.
/// </summary>
public static class DCBundleSerializer
{
    // The data files' own writing options, so that an exported creature reads
    // exactly as it does in creatures.json.
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static byte[] Write(DCBundle bundle)
    {
        return new UTF8Encoding(false).GetBytes(JsonSerializer.Serialize(bundle, WriteOptions));
    }

    /// <exception cref="DCBundleReadException">The file is refused; the error says why.</exception>
    public static DCBundle Read(byte[] bytes)
    {
        if (bytes.LongLength > DCBundle.MaxSizeBytes)
        {
            throw new DCBundleReadException(DCBundleReadError.FileTooLarge, $"{bytes.LongLength} bytes");
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(bytes, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException)
        {
            throw Unreadable("Not JSON", ex);
        }

        DCBundle bundle;
        try
        {
            bundle = root switch
            {
                JsonObject obj when obj.ContainsKey("Format") || obj.ContainsKey("format") => ReadEnvelope(obj),
                JsonObject obj => FromLegacy(new JsonArray(obj.DeepClone()), single: true),
                JsonArray array => FromLegacy(array, single: false),
                _ => throw Unreadable("Neither an object nor a list"),
            };
        }
        catch (DCBundleReadException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException or ArgumentException or FormatException)
        {
            throw Unreadable(ex.Message, ex);
        }

        Validate(bundle);
        return bundle;
    }

    private static DCBundle ReadEnvelope(JsonObject obj)
    {
        string? format = (obj["Format"] ?? obj["format"])?.GetValue<string>();
        if (format == DCBundle.OtherAppFormatName)
        {
            throw new DCBundleReadException(DCBundleReadError.OtherAppFile, format);
        }
        if (format != DCBundle.FormatName)
        {
            throw Unreadable($"Format '{format}'");
        }

        // A missing version is version 1. Older versions are upgraded here,
        // one step at a time, before the typed reading; version 1 has none.
        var versionNode = obj["FormatVersion"] ?? obj["formatVersion"];
        int version = versionNode == null ? 1 : versionNode.GetValue<int>();
        if (version > DCBundle.CurrentVersion)
        {
            throw new DCBundleReadException(DCBundleReadError.NewerFormat, $"Version {version}");
        }
        obj["FormatVersion"] = DCBundle.CurrentVersion;

        return obj.Deserialize<DCBundle>(ReadOptions) ?? throw Unreadable("Empty bundle");
    }

    // An export from before bundles: one creature, one combat, or a list of
    // either, told apart by JsonMitigatorId (1 creature, 2 combat).
    private static DCBundle FromLegacy(JsonArray items, bool single)
    {
        var bundle = new DCBundle { AppVersion = "", ExportedAtUtc = "" };
        bundle.Warnings.Add(new BundleWarning { Code = DCBundleWarningCodes.LegacyFormat });
        if (items.Count == 0)
        {
            return bundle;
        }

        int kind = MitigatorIdOf(items[0]);
        if (items.Any(i => MitigatorIdOf(i) != kind))
        {
            throw Unreadable("A list mixing creatures and combats");
        }

        if (kind == (int)DCJsonMitigatorId.CREATURE)
        {
            foreach (var item in items)
            {
                var creature = item!.Deserialize<DCCreature>(ReadOptions) ?? throw Unreadable("Empty creature");
                bundle.Creatures.Add(creature);
                bundle.Roots.Add(new BundleRoot { Kind = DCBundleKinds.Creature, Id = creature.Id });
            }
        }
        else if (kind == (int)DCJsonMitigatorId.COMBAT)
        {
            foreach (var item in items)
            {
                var combat = item!.Deserialize<DCCombat>(ReadOptions) ?? throw Unreadable("Empty combat");
                bundle.Combats.Add(combat);
                bundle.Roots.Add(new BundleRoot { Kind = DCBundleKinds.Combat, Id = combat.Id });

                // The creatures travel inside the combat: their copies, at
                // full health, stand in for the library versions.
                foreach (var entry in combat.CreaturesList)
                {
                    if (bundle.Creatures.Any(c => c.Id == entry.Id)) continue;
                    var creature = (DCCreature)entry.Clone();
                    foreach (var hitPoints in creature.CustomFields.OfType<DCCreatureHitPointsField>())
                    {
                        hitPoints.ResetForCombat();
                    }
                    bundle.Creatures.Add(creature);
                }
            }
        }
        else
        {
            throw Unreadable(single ? "Neither a creature nor a combat" : "A list of neither creatures nor combats");
        }
        return bundle;
    }

    private static int MitigatorIdOf(JsonNode? node)
    {
        var id = node is JsonObject obj ? (obj["JsonMitigatorId"] ?? obj["jsonMitigatorId"]) : null;
        if (id == null) return -1;
        try { return id.GetValue<int>(); }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException) { return -1; }
    }

    private static void Validate(DCBundle bundle)
    {
        bundle.Roots ??= new();
        bundle.Creatures ??= new();
        bundle.Combats ??= new();
        bundle.PlayerGroups ??= new();
        bundle.FieldTemplates ??= new();
        bundle.ExternalApps ??= new();
        bundle.Automations ??= new();
        bundle.Warnings ??= new();

        // Identity is unique within each kind.
        RequireUnique(bundle.Creatures.Select(c => c.Id.ToString()), "creature");
        RequireUnique(bundle.Combats.Select(c => c.Id.ToString()), "combat");
        RequireUnique(bundle.PlayerGroups.Select(g => g.Id.ToString()), "player group");
        RequireUnique(bundle.FieldTemplates.Select(f => f.Title), "user custom field");
        RequireUnique(bundle.ExternalApps.Select(a => a.Id.ToString()), "external app");
        RequireUnique(bundle.Automations.Select(a => a.Id.ToString()), "automation");

        // References that do not resolve are kept as they are and counted
        // once. A combat entry the exporter already reported as not in its
        // library is not counted again.
        var creatureIds = bundle.Creatures.Select(c => c.Id).ToHashSet();
        var appIds = bundle.ExternalApps.Select(a => a.Id).ToHashSet();
        var reportedEntries = bundle.Warnings
            .Where(w => w.Code == DCBundleWarningCodes.CreatureNotInLibrary && w.Id != null)
            .Select(w => (w.Id!.Value, w.Detail ?? ""))
            .ToHashSet();

        int broken = 0;
        foreach (var combat in bundle.Combats)
        {
            broken += combat.CreaturesList.Count(e => !creatureIds.Contains(e.Id) && !reportedEntries.Contains((combat.Id, e.Name)));
        }
        foreach (var group in bundle.PlayerGroups)
        {
            broken += group.PlayerIds.Count(id => !creatureIds.Contains(id));
        }
        foreach (var binding in bundle.Automations)
        {
            broken += binding.Actions.OfType<HttpRequestActionDefinition>().Count(a => a.ExternalAppId is Guid id && !appIds.Contains(id));
        }
        if (broken > 0)
        {
            bundle.Warnings.Add(new BundleWarning { Code = DCBundleWarningCodes.BrokenReferences, Detail = broken.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        }
    }

    private static void RequireUnique(IEnumerable<string> keys, string kind)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string key in keys)
        {
            if (!seen.Add(key)) throw Unreadable($"The {kind} {key} appears twice");
        }
    }

    private static DCBundleReadException Unreadable(string message, Exception? inner = null) =>
        new(DCBundleReadError.UnreadableFile, message, inner);
}

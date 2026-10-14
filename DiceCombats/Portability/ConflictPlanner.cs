/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using DiceCombats.Automations;
using DCImportItem = DiceCombats.Portability.ImportItem<DiceCombats.Portability.DCAssetKind>;
using DCImportPlan = DiceCombats.Portability.ImportPlan<DiceCombats.Portability.DCAssetKind>;

namespace DiceCombats.Portability;

/// <summary>The kinds of DiceCombats asset (spec §3.1), in the order an import applies them.</summary>
public enum DCAssetKind { FieldTemplate, ExternalApp, Creature, PlayerGroup, Combat, Automation }

/// <summary>What the library holds, read-only, for the planner.</summary>
public interface ILocalLibrary
{
    IReadOnlyList<DCCreature> Creatures { get; }
    IReadOnlyList<DCCombat> Combats { get; }
    IReadOnlyList<DCPlayerGroup> PlayerGroups { get; }
    IReadOnlyList<DCCreatureCustomField> FieldTemplates { get; }
    IReadOnlyList<ExternalAppDefinition> ExternalApps { get; }
    IReadOnlyList<EventActionBinding> Automations { get; }
}

public sealed record DCLocalLibrary(
    IReadOnlyList<DCCreature> Creatures, IReadOnlyList<DCCombat> Combats, IReadOnlyList<DCPlayerGroup> PlayerGroups,
    IReadOnlyList<DCCreatureCustomField> FieldTemplates, IReadOnlyList<ExternalAppDefinition> ExternalApps,
    IReadOnlyList<EventActionBinding> Automations) : ILocalLibrary;

/// <summary>
/// Compares each asset of a bundle with the library and sets the choices of
/// workplan §1.1 (spec §4.1, §4.2). Writes nothing.
/// </summary>
public sealed class ConflictPlanner
{
    private readonly ILocalLibrary _local;

    public ConflictPlanner(ILocalLibrary local)
    {
        _local = local;
    }

    public DCImportPlan Analyse(DCBundle bundle)
    {
        var plan = new DCImportPlan();
        plan.Warnings.AddRange(bundle.Warnings);

        foreach (var template in bundle.FieldTemplates)
        {
            var local = _local.FieldTemplates.FirstOrDefault(t => SameName(t.Title, template.Title));
            var status = local == null ? ImportStatus.New
                : DCFingerprint.Of(local) == DCFingerprint.Of(template) ? ImportStatus.UpToDate : ImportStatus.Differs;
            Add(plan, DCAssetKind.FieldTemplate, template, local, null, template.Title, status);
        }
        foreach (var app in bundle.ExternalApps)
        {
            AddById(plan, DCAssetKind.ExternalApp, app, app.Id, app.Name, _local.ExternalApps, a => a.Id, a => a.Name, DCFingerprint.Of);
        }
        foreach (var creature in bundle.Creatures)
        {
            AddById(plan, DCAssetKind.Creature, creature, creature.Id, creature.Name, _local.Creatures, c => c.Id, c => c.Name, DCFingerprint.Of);
        }
        foreach (var group in bundle.PlayerGroups)
        {
            AddById(plan, DCAssetKind.PlayerGroup, group, group.Id, group.Name, _local.PlayerGroups, g => g.Id, g => g.Name, DCFingerprint.Of);
        }
        foreach (var combat in bundle.Combats)
        {
            AddById(plan, DCAssetKind.Combat, combat, combat.Id, combat.Name, _local.Combats, c => c.Id, c => c.Name, DCFingerprint.Of);
        }
        foreach (var binding in bundle.Automations)
        {
            AddById(plan, DCAssetKind.Automation, binding, binding.Id, binding.Name, _local.Automations, b => b.Id, b => b.Name, DCFingerprint.Of);
        }
        return plan;
    }

    // Kinds identified by GUID: a GUID match wins over a name match.
    private void AddById<T>(DCImportPlan plan, DCAssetKind kind, T incoming, Guid id, string name, IReadOnlyList<T> locals,
                            Func<T, Guid> idOf, Func<T, string> nameOf, Func<T, string> fingerprint) where T : class
    {
        var byId = locals.FirstOrDefault(l => idOf(l) == id);
        if (byId != null)
        {
            Add(plan, kind, incoming, byId, id, name, fingerprint(byId) == fingerprint(incoming) ? ImportStatus.UpToDate : ImportStatus.Differs);
            return;
        }
        var byName = locals.FirstOrDefault(l => SameName(nameOf(l), name));
        Add(plan, kind, incoming, byName, id, name, byName == null ? ImportStatus.New : ImportStatus.NameUsed);
    }

    private void Add(DCImportPlan plan, DCAssetKind kind, object incoming, object? local, Guid? id, string name, ImportStatus status)
    {
        var item = new DCImportItem
        {
            Kind = kind,
            Incoming = incoming,
            Local = local,
            IncomingId = id,
            IncomingName = name,
            Status = status,
            AllowedChoices = ChoicesFor(kind, status),
            Choice = DefaultChoice(kind, status),
        };
        plan.Items.Add(item);
        if (item.NeedsNewName)
        {
            item.NewName = MakeUniqueName(plan, item, name);
        }
    }

    public static IReadOnlyList<ImportChoice> ChoicesFor(DCAssetKind kind, ImportStatus status) => status switch
    {
        ImportStatus.New => new[] { ImportChoice.Import, ImportChoice.Skip },
        ImportStatus.UpToDate => new[] { ImportChoice.Nothing },
        // DiceCombats has no container to merge.
        ImportStatus.Differs => new[] { ImportChoice.ImportAsCopy, ImportChoice.ReplaceLocal, ImportChoice.KeepLocal },
        // Never for a user custom field, which is identified by its title.
        _ => new[] { ImportChoice.ImportWithNewName, ImportChoice.KeepBothSameName, ImportChoice.UseLocal, ImportChoice.Skip },
    };

    public static ImportChoice DefaultChoice(DCAssetKind kind, ImportStatus status) => status switch
    {
        ImportStatus.New => ImportChoice.Import,
        ImportStatus.UpToDate => ImportChoice.Nothing,
        ImportStatus.Differs => ImportChoice.ImportAsCopy,
        _ => ImportChoice.ImportWithNewName,
    };

    /// <summary>Sets a choice, and a unique new name when the choice needs one.</summary>
    public void SetChoice(DCImportPlan plan, DCImportItem item, ImportChoice choice)
    {
        if (!item.AllowedChoices.Contains(choice)) return;
        item.Choice = choice;
        if (item.NeedsNewName && (item.NewName == null || !IsNameAvailable(plan, item, item.NewName)))
        {
            item.NewName = MakeUniqueName(plan, item, item.IncomingName);
        }
    }

    /// <summary>
    /// A new name must not be empty and must be unique among the library's
    /// assets of the same kind and the other names this import creates
    /// (trimmed, case ignored).
    /// </summary>
    public bool IsNameAvailable(DCImportPlan plan, DCImportItem item, string name)
    {
        string trimmed = (name ?? "").Trim();
        if (trimmed.Length == 0) return false;
        if (LocalNames(item.Kind).Any(n => SameName(n, trimmed))) return false;
        return !plan.AllItems.Any(other => !ReferenceEquals(other, item) && other.Kind == item.Kind
                                           && CreatedName(other) is string created && SameName(created, trimmed));
    }

    /// <summary>The wanted name, or "name (2)", "name (3)"… until it is available.</summary>
    public string MakeUniqueName(DCImportPlan plan, DCImportItem item, string wanted)
    {
        string baseName = (wanted ?? "").Trim();
        if (baseName.Length == 0) baseName = "?";
        if (IsNameAvailable(plan, item, baseName)) return baseName;
        for (int n = 2; ; n++)
        {
            string candidate = $"{baseName} ({n})";
            if (IsNameAvailable(plan, item, candidate)) return candidate;
        }
    }

    /// <summary>The items whose new name is empty or taken; the import is not applied while there is one.</summary>
    public IReadOnlyList<DCImportItem> InvalidNewNames(DCImportPlan plan) =>
        plan.AllItems.Where(i => i.NeedsNewName && (i.NewName == null || !IsNameAvailable(plan, i, i.NewName))).ToList();

    /// <summary>"Apply to all of this kind": the same choice on every item of this kind and status that allows it.</summary>
    public void ApplyToAllOfKind(DCImportPlan plan, DCAssetKind kind, ImportStatus status, ImportChoice choice)
    {
        foreach (var item in plan.AllItems.Where(i => i.Kind == kind && i.Status == status && i.AllowedChoices.Contains(choice)))
        {
            item.Choice = choice;
            item.NewName = null;
        }
        foreach (var item in plan.AllItems.Where(i => i.Kind == kind && i.Status == status && i.NeedsNewName))
        {
            item.NewName = MakeUniqueName(plan, item, item.IncomingName);
        }
    }

    /// <summary>The items this import inserts that use the given one (to warn before it is skipped).</summary>
    public IReadOnlyList<DCImportItem> DependentsOf(DCImportPlan plan, DCImportItem item)
    {
        if (item.IncomingId is not Guid id) return Array.Empty<DCImportItem>();
        return plan.AllItems.Where(other => other.Inserts && item.Kind switch
        {
            DCAssetKind.Creature => other.Incoming is DCCombat combat && combat.CreaturesList.Any(e => e.Id == id)
                                    || other.Incoming is DCPlayerGroup group && group.PlayerIds.Contains(id),
            DCAssetKind.ExternalApp => other.Incoming is EventActionBinding binding
                                       && binding.Actions.OfType<HttpRequestActionDefinition>().Any(a => a.ExternalAppId == id),
            _ => false,
        }).ToList();
    }

    /// <summary>The name an asset shows in the wizard.</summary>
    public static string NameOf(object? asset) => asset switch
    {
        DCCreature c => c.Name,
        DCCombat c => c.Name,
        DCPlayerGroup g => g.Name,
        DCCreatureCustomField f => f.Title,
        ExternalAppDefinition a => a.Name,
        EventActionBinding b => b.Name,
        _ => "",
    };

    private IEnumerable<string> LocalNames(DCAssetKind kind) => kind switch
    {
        DCAssetKind.Creature => _local.Creatures.Select(c => c.Name),
        DCAssetKind.Combat => _local.Combats.Select(c => c.Name),
        DCAssetKind.PlayerGroup => _local.PlayerGroups.Select(g => g.Name),
        DCAssetKind.FieldTemplate => _local.FieldTemplates.Select(f => f.Title),
        DCAssetKind.ExternalApp => _local.ExternalApps.Select(a => a.Name),
        _ => _local.Automations.Select(b => b.Name),
    };

    // The name an item will create in the library, if it creates one under a
    // name of its own choosing (an import keeping its name is not a rename).
    private static string? CreatedName(DCImportItem item) => item.Choice switch
    {
        ImportChoice.ImportAsCopy or ImportChoice.ImportWithNewName => item.NewName,
        ImportChoice.Import => item.IncomingName,
        _ => null,
    };

    private static bool SameName(string a, string b) =>
        string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.CurrentCultureIgnoreCase);
}

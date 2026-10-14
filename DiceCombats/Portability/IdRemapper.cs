/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using DiceCombats.Automations;
using DCImportPlan = DiceCombats.Portability.ImportPlan<DiceCombats.Portability.DCAssetKind>;

namespace DiceCombats.Portability;

/// <summary>
/// The GUID map of an import (spec §4.3): which incoming GUID becomes which,
/// and the rewriting of every reference inside the bundle with it, before
/// anything is inserted.
/// </summary>
public sealed class IdRemapper
{
    private readonly Dictionary<Guid, Guid> _map = new();

    public IReadOnlyDictionary<Guid, Guid> Map => _map;

    /// <exception cref="InvalidOperationException">'from' is already mapped to another GUID.</exception>
    public void Add(Guid from, Guid to)
    {
        if (_map.TryGetValue(from, out var existing) && existing != to)
        {
            throw new InvalidOperationException($"{from} is already mapped to {existing}.");
        }
        _map[from] = to;
    }

    public Guid Resolve(Guid id) => _map.TryGetValue(id, out var to) ? to : id;

    public Guid? Resolve(Guid? id) => id is Guid value ? Resolve(value) : null;

    /// <summary>
    /// Import as a copy: a new GUID. Use the local one instead: the local
    /// asset's GUID, so that everything imported that uses it points at the
    /// local one. Every other choice keeps the GUID.
    /// </summary>
    public static IdRemapper BuildFrom(DCImportPlan plan, Func<Guid>? newGuid = null)
    {
        newGuid ??= Guid.NewGuid;
        var remapper = new IdRemapper();
        foreach (var item in plan.AllItems)
        {
            if (item.IncomingId is not Guid id) continue;
            switch (item.Choice)
            {
                case ImportChoice.ImportAsCopy:
                    remapper.Add(id, newGuid());
                    break;
                case ImportChoice.UseLocal when item.Local != null:
                    remapper.Add(id, LocalIdOf(item.Local));
                    break;
                default:
                    remapper.Add(id, id);
                    break;
            }
        }
        return remapper;
    }

    /// <summary>Rewrites, in place, every GUID of the bundle: the assets' own and every reference (spec §3.1).</summary>
    public void Rewrite(DCBundle bundle)
    {
        foreach (var creature in bundle.Creatures) creature.Id = Resolve(creature.Id);
        foreach (var combat in bundle.Combats)
        {
            combat.Id = Resolve(combat.Id);
            foreach (var entry in combat.CreaturesList) entry.Id = Resolve(entry.Id);
        }
        foreach (var group in bundle.PlayerGroups)
        {
            group.Id = Resolve(group.Id);
            group.PlayerIds = group.PlayerIds.Select(Resolve).ToList();
        }
        foreach (var app in bundle.ExternalApps) app.Id = Resolve(app.Id);
        foreach (var binding in bundle.Automations)
        {
            binding.Id = Resolve(binding.Id);
            foreach (var http in binding.Actions.OfType<HttpRequestActionDefinition>()) http.ExternalAppId = Resolve(http.ExternalAppId);
        }
        foreach (var root in bundle.Roots) root.Id = Resolve(root.Id);
    }

    private static Guid LocalIdOf(object local) => local switch
    {
        DCCreature c => c.Id,
        DCCombat c => c.Id,
        DCPlayerGroup g => g.Id,
        ExternalAppDefinition a => a.Id,
        EventActionBinding b => b.Id,
        _ => throw new ArgumentException($"{local.GetType().Name} has no GUID."),
    };
}

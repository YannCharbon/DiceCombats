/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceCombats
{
    /// <summary>One distinct creature of a combat, with how many of it there are.</summary>
    public readonly record struct DCRosterEntry(string Name, int Count);

    /// <summary>
    /// A combat's opposition, one entry per distinct creature name.
    ///
    /// A combat holds a single <see cref="DCCreature"/> per creature and says
    /// how many of it are in the fight through
    /// <see cref="DCCreature.InCombatInstanceCount"/>, so counting list entries
    /// would report four goblins as one. Everything here counts instances.
    ///
    /// The party is counted rather than listed: every combat has roughly the
    /// same party, so listing it would drown out what is actually being fought.
    /// </summary>
    public static class DCCombatRoster
    {
        /// <summary>Instances of one creature, never less than one.</summary>
        private static int Instances(DCCreature creature) =>
            (int)Math.Max(1u, creature.InCombatInstanceCount);

        /// <summary>Distinct non-player creatures, the most numerous first.</summary>
        public static List<DCRosterEntry> Of(DCCombat combat)
        {
            if (combat.CreaturesList.Count == 0) return new List<DCRosterEntry>();

            return combat.CreaturesList
                .Where(c => !c.IsPlayer)
                .GroupBy(c => c.Name, StringComparer.CurrentCulture)
                .Select(g => new DCRosterEntry(g.Key, g.Sum(Instances)))
                .OrderByDescending(e => e.Count)
                .ThenBy(e => e.Name, StringComparer.CurrentCulture)
                .ToList();
        }

        /// <summary>How many creatures are in the fight, instances included.</summary>
        public static int CreatureCount(DCCombat combat) =>
            combat.CreaturesList.Where(c => !c.IsPlayer).Sum(Instances);

        public static int PlayerCount(DCCombat combat) =>
            combat.CreaturesList.Where(c => c.IsPlayer).Sum(Instances);
    }
}

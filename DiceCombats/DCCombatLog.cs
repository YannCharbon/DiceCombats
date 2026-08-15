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
    public static class DCCombatLogEntryTypes
    {
        public const string Damage = "Damage";
        public const string Heal = "Heal";
        public const string Turn = "Turn";
    }

    /// <summary>
    /// One recorded moment of a fight. Names rather than ids, so a log stays
    /// readable after a creature has been renamed or removed.
    /// </summary>
    public class DCCombatLogEntry
    {
        public int Sequence { get; set; }
        public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
        public string Type { get; set; } = DCCombatLogEntryTypes.Damage;
        public int Round { get; set; } = 1;

        public string SourceName { get; set; } = string.Empty;
        // True when nobody picked the source and it was taken from whoever held
        // the initiative, which the statistics report separately.
        public bool SourceInferred { get; set; }

        public string TargetName { get; set; } = string.Empty;
        public int InstanceIndex { get; set; }

        public string DamageKind { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public int RawAmount { get; set; }
        public int AppliedAmount { get; set; }
    }

    /// <summary>
    /// One play of a combat, from pressing play to stopping or closing the app.
    /// Playing the same combat again records a new session.
    /// </summary>
    public class DCCombatLogSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CombatId { get; set; } = Guid.Empty;
        public string CombatName { get; set; } = string.Empty;
        public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? EndedAt { get; set; }
        public List<DCCombatLogEntry> Entries { get; set; } = new List<DCCombatLogEntry>();

        public int Rounds => Entries.Count == 0 ? 0 : Entries.Max(x => x.Round);
    }

    /// <summary>
    /// What one creature did over a session. Built by <see cref="DCCombatLogStatistics"/>.
    /// </summary>
    public class DCCombatLogSourceSummary
    {
        public string SourceName { get; set; } = string.Empty;
        public int DamageDealt { get; set; }
        public int HealingGiven { get; set; }
        public int Hits { get; set; }
        public int InferredHits { get; set; }
        public Dictionary<string, int> DamageByKind { get; set; } = new Dictionary<string, int>();

        public int BestHit { get; set; }
        public double AverageHit => Hits == 0 ? 0 : (double)DamageDealt / Hits;
    }

    public class DCCombatLogTargetSummary
    {
        public string TargetName { get; set; } = string.Empty;
        public int DamageTaken { get; set; }
        public int HealingReceived { get; set; }
    }

    /// <summary>
    /// Read-only aggregation of a session. Kept out of the session itself so the
    /// stored file holds facts only and the numbers are always recomputed.
    /// </summary>
    public static class DCCombatLogStatistics
    {
        public static List<DCCombatLogSourceSummary> BySource(DCCombatLogSession session)
        {
            var summaries = new Dictionary<string, DCCombatLogSourceSummary>();

            foreach (var entry in session.Entries)
            {
                if (entry.Type != DCCombatLogEntryTypes.Damage && entry.Type != DCCombatLogEntryTypes.Heal) continue;
                if (string.IsNullOrEmpty(entry.SourceName)) continue;

                if (!summaries.TryGetValue(entry.SourceName, out var summary))
                {
                    summary = new DCCombatLogSourceSummary { SourceName = entry.SourceName };
                    summaries[entry.SourceName] = summary;
                }

                if (entry.Type == DCCombatLogEntryTypes.Heal)
                {
                    summary.HealingGiven += entry.AppliedAmount;
                    continue;
                }

                summary.DamageDealt += entry.AppliedAmount;
                summary.Hits++;
                if (entry.SourceInferred) summary.InferredHits++;
                if (entry.AppliedAmount > summary.BestHit) summary.BestHit = entry.AppliedAmount;

                string kind = string.IsNullOrEmpty(entry.DamageKind) ? "?" : entry.DamageKind;
                summary.DamageByKind.TryGetValue(kind, out int kindTotal);
                summary.DamageByKind[kind] = kindTotal + entry.AppliedAmount;
            }

            return summaries.Values.OrderByDescending(x => x.DamageDealt).ToList();
        }

        public static List<DCCombatLogTargetSummary> ByTarget(DCCombatLogSession session)
        {
            var summaries = new Dictionary<string, DCCombatLogTargetSummary>();

            foreach (var entry in session.Entries)
            {
                if (string.IsNullOrEmpty(entry.TargetName)) continue;

                if (!summaries.TryGetValue(entry.TargetName, out var summary))
                {
                    summary = new DCCombatLogTargetSummary { TargetName = entry.TargetName };
                    summaries[entry.TargetName] = summary;
                }

                if (entry.Type == DCCombatLogEntryTypes.Damage) summary.DamageTaken += entry.AppliedAmount;
                else if (entry.Type == DCCombatLogEntryTypes.Heal) summary.HealingReceived += entry.AppliedAmount;
            }

            return summaries.Values.OrderByDescending(x => x.DamageTaken).ToList();
        }
    }
}

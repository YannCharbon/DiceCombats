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
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DiceCombats
{
    public class DCCombat
    {
        public DCJsonMitigatorId JsonMitigatorId { get; set; } = DCJsonMitigatorId.COMBAT;
        public Guid Id { get; set; } = Guid.Empty;
        public string Name { get; set; } = string.Empty;
        // Empty means the combat sits outside any campaign, listed on its own in
        // the explorer. Campaigns are plain names, not entities: a campaign
        // exists exactly as long as at least one combat carries its name.
        public string Campaign { get; set; } = string.Empty;
        public string Notes {  get; set; } = string.Empty;
        public int DurationCount { get; set; } = 0;
        public List<DCCreature> CreaturesList { get; set; } = new List<DCCreature>();

        public List<List<List<DCCreatureCustomField>>> InCombatCustomFieldsInstances { get; set; } = new List<List<List<DCCreatureCustomField>>>();

        public DCCombat(string name)
        {
            Name = name;
            Id = Guid.NewGuid();
        }

        public void AddCreature(DCCreature creature)
        {
            var creatureClone = (DCCreature)creature.Clone();
            CreaturesList.Add(creatureClone);
            ResetHitPointsForCombat(creatureClone.CustomFields);
            InCombatCustomFieldsInstances.Add(BuildInstances(creatureClone));
        }

        /// One fresh set of fields per instance, at full health.
        private static List<List<DCCreatureCustomField>> BuildInstances(DCCreature creature)
        {
            List<List<DCCreatureCustomField>> instances = new List<List<DCCreatureCustomField>>();
            for (int i = 0; i < creature.InCombatInstanceCount; i++)
            {
                List<DCCreatureCustomField> fields = new List<DCCreatureCustomField>();
                foreach (var field in creature.CustomFields)
                {
                    fields.Add(field.Clone());
                }
                ResetHitPointsForCombat(fields);
                instances.Add(fields);
            }
            return instances;
        }

        // Linking library creatures and combat entries (workplan 5.2).
        // An entry is a copy of a library creature and keeps its GUID. Names
        // are not unique, so the GUID decides; the name is only a fallback for
        // an entry whose creature is no longer in the library (for example one
        // deleted and made again), and only when exactly one library creature
        // carries that name.

        /// <summary>The entry of this combat that stands for a library creature, or null.</summary>
        public DCCreature? FindEntryFor(DCCreature libraryCreature, IReadOnlyList<DCCreature> library)
        {
            DCCreature? byId = CreaturesList.Find(x => x.Id == libraryCreature.Id);
            if (byId != null)
            {
                return byId;
            }
            if (CountByName(library, libraryCreature.Name) != 1)
            {
                return null;
            }
            return CreaturesList.Find(x => x.Name == libraryCreature.Name && !ContainsId(library, x.Id));
        }

        /// <summary>
        /// The library creature an entry of this combat stands for, or null. A
        /// name match is ignored when another entry already holds the GUID of
        /// that library creature, so that two entries never become the same one.
        /// </summary>
        public DCCreature? FindLibraryCreatureFor(DCCreature entry, IReadOnlyList<DCCreature> library)
        {
            DCCreature? byId = library.FirstOrDefault(x => x.Id == entry.Id);
            if (byId != null)
            {
                return byId;
            }
            if (CountByName(library, entry.Name) != 1)
            {
                return null;
            }
            DCCreature byName = library.First(x => x.Name == entry.Name);
            return CreaturesList.Exists(x => x != entry && x.Id == byName.Id) ? null : byName;
        }

        private static int CountByName(IReadOnlyList<DCCreature> library, string name)
        {
            int count = 0;
            foreach (DCCreature creature in library)
            {
                if (creature.Name == name && ++count > 1) break;
            }
            return count;
        }

        private static bool ContainsId(IReadOnlyList<DCCreature> library, Guid id)
        {
            foreach (DCCreature creature in library)
            {
                if (creature.Id == id) return true;
            }
            return false;
        }

        /// <summary>
        /// Brings every entry up to date with its library creature, in place:
        /// the entry keeps its position, instance count and initiative, takes
        /// the library GUID, and its instances start again at full health. The
        /// library is not changed. Entries that match no library creature are
        /// left as they are and returned. When <paramref name="replacedIds"/> is
        /// given, it receives old GUID to new GUID for each entry whose GUID changed.
        /// </summary>
        public List<DCCreature> SyncWithLibrary(IReadOnlyList<DCCreature> library, IDictionary<Guid, Guid>? replacedIds = null)
        {
            List<DCCreature> notFound = new List<DCCreature>();
            for (int i = 0; i < CreaturesList.Count; i++)
            {
                DCCreature entry = CreaturesList[i];
                DCCreature? libraryCreature = FindLibraryCreatureFor(entry, library);
                if (libraryCreature == null)
                {
                    notFound.Add(entry);
                    continue;
                }

                var updated = (DCCreature)libraryCreature.Clone();
                updated.InCombatInstanceCount = entry.InCombatInstanceCount;
                updated.InitiativeRoll = entry.InitiativeRoll;
                updated.InitiativeRank = entry.InitiativeRank;
                ResetHitPointsForCombat(updated.CustomFields);

                if (entry.Id != updated.Id && replacedIds != null)
                {
                    replacedIds[entry.Id] = updated.Id;
                }

                CreaturesList[i] = updated;
                if (i < InCombatCustomFieldsInstances.Count)
                {
                    InCombatCustomFieldsInstances[i] = BuildInstances(updated);
                }
                else
                {
                    // A combat saved with fewer instance lists than entries:
                    // the missing ones are made, so that every entry has its own.
                    while (InCombatCustomFieldsInstances.Count < i)
                    {
                        InCombatCustomFieldsInstances.Add(BuildInstances(CreaturesList[InCombatCustomFieldsInstances.Count]));
                    }
                    InCombatCustomFieldsInstances.Add(BuildInstances(updated));
                }
            }
            return notFound;
        }

        /// <summary>
        /// A creature joins a fight at full health, whatever the sheet it was
        /// copied from happened to be left at.
        /// </summary>
        private static void ResetHitPointsForCombat(List<DCCreatureCustomField> fields)
        {
            foreach (var field in fields)
            {
                if (field is DCCreatureHitPointsField hitPoints)
                {
                    hitPoints.ResetForCombat();
                }
            }
        }

        /// <summary>
        /// Removes an entry of this combat, found as this very object or else by
        /// its GUID. Returns false, and changes nothing, when it is not there.
        /// </summary>
        public bool RemoveCreature(DCCreature entry)
        {
            int creatureIdx = CreaturesList.IndexOf(entry);
            if (creatureIdx == -1)
            {
                creatureIdx = CreaturesList.FindIndex(x => x.Id == entry.Id);
            }
            if (creatureIdx == -1)
            {
                return false;
            }
            if (creatureIdx < InCombatCustomFieldsInstances.Count)
            {
                InCombatCustomFieldsInstances.RemoveAt(creatureIdx);
            }
            CreaturesList.RemoveAt(creatureIdx);
            return true;
        }

        public void MoveCreature(DCCreature creature, bool up)
        {
            int oldIndex = CreaturesList.IndexOf(creature);
            if (oldIndex != -1)
            {
                if (!up)
                {
                    if (oldIndex < CreaturesList.Count - 1)
                    {
                        CreaturesList.RemoveAt(oldIndex);
                        CreaturesList.Insert(oldIndex + 1, creature);

                        List<List<DCCreatureCustomField>> InCombatCustomFieldsInstance = InCombatCustomFieldsInstances[oldIndex];
                        InCombatCustomFieldsInstances.RemoveAt(oldIndex);
                        InCombatCustomFieldsInstances.Insert(oldIndex + 1, InCombatCustomFieldsInstance);
                    }
                }
                else
                {
                    if (oldIndex > 0)
                    {
                        CreaturesList.RemoveAt(oldIndex);
                        CreaturesList.Insert(oldIndex - 1, creature);

                        List<List<DCCreatureCustomField>> InCombatCustomFieldsInstance = InCombatCustomFieldsInstances[oldIndex];
                        InCombatCustomFieldsInstances.RemoveAt(oldIndex);
                        InCombatCustomFieldsInstances.Insert(oldIndex - 1, InCombatCustomFieldsInstance);
                    }
                }
            }
        }

        public void MoveCreature(DCCreature creature, int newIndex)
        {
            int oldIndex = CreaturesList.IndexOf(creature);
            if (oldIndex != -1)
            {
                CreaturesList.RemoveAt(oldIndex);
                CreaturesList.Insert(newIndex, creature);

                List<List<DCCreatureCustomField>> InCombatCustomFieldsInstance = InCombatCustomFieldsInstances[oldIndex];
                InCombatCustomFieldsInstances.RemoveAt(oldIndex);
                InCombatCustomFieldsInstances.Insert(newIndex, InCombatCustomFieldsInstance);
            }
        }

        public List<List<DCCreatureCustomField>> GetCreatureInstancesCustomFields(DCCreature creature)
        {
            int creatureIdx = CreaturesList.IndexOf(creature);
            if (creatureIdx != -1 && creatureIdx < InCombatCustomFieldsInstances.Count())
            {
                return InCombatCustomFieldsInstances[creatureIdx];
            }
            return new List<List<DCCreatureCustomField>>();
        }

        public void UpdateInstanceCount(uint instanceCount, DCCreature creature)
        {
            var creatureClone = (DCCreature)creature.Clone();
            int creatureIdx = CreaturesList.IndexOf(creature);
            if (creatureIdx != -1)
            {
                InCombatCustomFieldsInstances[creatureIdx].Clear();
                for (int i = 0; i < instanceCount; i++)
                {
                    List<DCCreatureCustomField> fields = new List<DCCreatureCustomField>();
                    foreach (var field in creatureClone.CustomFields)
                    {
                        fields.Add(field.Clone());
                    }
                    InCombatCustomFieldsInstances[creatureIdx].Add(fields);
                }
                creature.InCombatInstanceCount = instanceCount;
            }
            
        }
    }
}

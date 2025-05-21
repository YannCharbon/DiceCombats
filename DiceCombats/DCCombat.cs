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
            List<List<DCCreatureCustomField>> tmp = new List<List<DCCreatureCustomField>>();
            for (int i = 0; i < creatureClone.InCombatInstanceCount; i++)
            {
                List<DCCreatureCustomField> fields = new List<DCCreatureCustomField>();
                foreach (var field in creatureClone.CustomFields)
                {
                    fields.Add(field.Clone());
                }
                tmp.Add(fields);
            }
            InCombatCustomFieldsInstances.Add(tmp);
        }

        public void RemoveCreature(DCCreature creature)
        {
            int creatureIdx = CreaturesList.FindIndex(x => x.Name == creature.Name);
            if (creatureIdx != -1 && creatureIdx < InCombatCustomFieldsInstances.Count())
            {
                InCombatCustomFieldsInstances.RemoveAt(creatureIdx);
            }
            CreaturesList.RemoveAt(creatureIdx);
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

        public void SyncCreatures()
        {
            foreach (DCCreature creature in CreaturesList.ToList())
            {
                RemoveCreature(creature);
                AddCreature(creature);
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

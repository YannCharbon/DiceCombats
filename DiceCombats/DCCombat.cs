using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiceCombats
{
    public class DCCombat
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Name { get; set; } = string.Empty;
        public List<DCCreature> CreaturesList { get; set; } = new List<DCCreature>();

        public List<List<List<DCCreatureCustomField>>> InCombatCustomFieldsInstances { get; set; } = new List<List<List<DCCreatureCustomField>>>();

        public DCCombat(string name)
        {
            Name = name;
            Id = Guid.NewGuid();
        }

        public void AddCreature(DCCreature creature)
        {
            CreaturesList.Add(creature);
            List<List<DCCreatureCustomField>> tmp = new List<List<DCCreatureCustomField>>();
            for (int i = 0; i < creature.InCombatInstanceCount; i++)
            {
                tmp.Add(creature.CustomFields.Select(item => item.Clone()).ToList());
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
                } else
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
            int creatureIdx = CreaturesList.IndexOf(creature);
            if (creatureIdx != -1)
            {
                InCombatCustomFieldsInstances[creatureIdx].Clear();
                for (int i = 0; i < instanceCount; i++)
                {
                    InCombatCustomFieldsInstances[creatureIdx].Add(creature.CustomFields.Select(item => item.Clone()).ToList());
                }
                creature.InCombatInstanceCount = instanceCount;
            }
            
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;

namespace DiceCombats
{
    public class DiceCombatsService
    {
        private List<DCCreature> _CreatureList = new List<DCCreature>();
        private List<DCCombat> _CombatsList = new List<DCCombat>();

        public DiceCombatsService()
        {
            LoadCreatureListAsync();
            LoadCombatsListAsync();
        }

        public DCCreature? GetCreatureFromGUID(string guid)
        {
            return _CreatureList.Find(x => x.Id.ToString() == guid);
        }

        public List<DCCreature> GetCreatureList() { return _CreatureList; }

        public void AddCreature(DCCreature creature)
        {
            _CreatureList.Add(creature);
            string json = JsonSerializer.Serialize(_CreatureList);
            Debug.WriteLine(json);
        }

        public void DeleteCreature(DCCreature creature)
        {
            _CreatureList.Remove(creature);
        }

        public void SaveCreatures()
        {
            var options = new JsonSerializerOptions
            {
                /*Converters = { new DCCreatureCustomFieldConverter() },*/
                WriteIndented = true // Optional: for better readability
            };
            string json = JsonSerializer.Serialize(_CreatureList, options);
            Debug.WriteLine(json);

            SaveCreatureListAsync(_CreatureList);
        }

        private static string creatureFilePath = Path.Combine(FileSystem.AppDataDirectory, "creatures.json");

        public async Task SaveCreatureListAsync(List<DCCreature> creatureList)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
            };
            string json = JsonSerializer.Serialize(creatureList, options);
            await File.WriteAllTextAsync(creatureFilePath, json);
            Debug.WriteLine($"Write file to {creatureFilePath}");
        }

        public async Task LoadCreatureListAsync()
        {
            if (File.Exists(creatureFilePath))
            {
                Debug.WriteLine("File exists");
                string json = await File.ReadAllTextAsync(creatureFilePath);
                var options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
                };
                var temp = JsonSerializer.Deserialize<List<DCCreature>>(json, options);
                if (temp != null)
                {
                    _CreatureList = temp;
                }
            }
        }


        // Combats

        public DCCombat? GetCombatFromGUID(string guid)
        {
            return _CombatsList.Find(x => x.Id.ToString() == guid);
        }

        public List<DCCombat> GetCombatList() { return _CombatsList; }

        public void AddCombat(DCCombat combat)
        {
            _CombatsList.Add(combat);
        }

        public void DeleteCombat(DCCombat combat)
        {
            _CombatsList.Remove(combat);
        }

        public void SaveCombats()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true // Optional: for better readability
            };
            string json = JsonSerializer.Serialize(_CombatsList, options);
            Debug.WriteLine(json);

            SaveCombatsListAsync(_CombatsList);
        }

        private static string combatsFilePath = Path.Combine(FileSystem.AppDataDirectory, "combats.json");

        public async Task SaveCombatsListAsync(List<DCCombat> combatsList)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
            };
            string json = JsonSerializer.Serialize(combatsList, options);
            await File.WriteAllTextAsync(combatsFilePath, json);
            Debug.WriteLine($"Write file to {combatsFilePath}");
        }

        public async Task LoadCombatsListAsync()
        {
            if (File.Exists(combatsFilePath))
            {
                Debug.WriteLine("File exists");
                string json = await File.ReadAllTextAsync(combatsFilePath);
                var options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
                };
                var temp = JsonSerializer.Deserialize<List<DCCombat>>(json, options);
                if (temp != null)
                {
                    _CombatsList = temp;
                }
            }
        }
    }
}

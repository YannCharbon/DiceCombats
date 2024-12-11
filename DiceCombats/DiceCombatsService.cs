using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using static MudBlazor.FilterOperator;

namespace DiceCombats
{
    public class DiceCombatsService
    {
        private List<DCCreature> _CreatureList = new List<DCCreature>();
        private List<DCCombat> _CombatsList = new List<DCCombat>();
        private List<string> _favoriteCombats = new List<string>();
        private List<DCCreatureCustomField> _userCreatureCustomFields = new List<DCCreatureCustomField>();

        public DiceCombatsService()
        {
            LoadCreatureListAsync();
            LoadCombatsListAsync();
            LoadFavoriteCombatsListAsync();
            LoadUserCreatureCustomFieldsListAsync();
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

        public void AddCombatToFavoriteByGUID(string guid)
        {
            if (_favoriteCombats.Contains(guid) == false)
            {
                _favoriteCombats.Add(guid);
                SaveFavoriteCombatsListAsync();
            }
        }

        public void RemoveCombatToFavoriteByGUID(string guid)
        {
            if (_favoriteCombats.Contains(guid) == true)
            {
                _favoriteCombats.Remove(guid);
                SaveFavoriteCombatsListAsync();
            }
        }

        public bool IsCombatFavorite(string guid)
        {
            return _favoriteCombats.Contains(guid);
        }

        public List<string> GetFavoriteCombatsGuids()
        {
            foreach (var guid in _favoriteCombats.ToArray())
            if (GetCombatFromGUID(guid) == null)
            {
                // Update the list if anything has changed
                RemoveCombatToFavoriteByGUID(guid);
            }
            return _favoriteCombats;
        }

        private static string favoriteCombatsFilePath = Path.Combine(FileSystem.AppDataDirectory, "favoritesCombats.json");

        public async Task SaveFavoriteCombatsListAsync()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
            };
            string json = JsonSerializer.Serialize(_favoriteCombats, options);
            await File.WriteAllTextAsync(favoriteCombatsFilePath, json);
            Debug.WriteLine($"Write file to {favoriteCombatsFilePath}");
        }

        public async Task LoadFavoriteCombatsListAsync()
        {
            if (File.Exists(favoriteCombatsFilePath))
            {
                Debug.WriteLine("File exists");
                string json = await File.ReadAllTextAsync(favoriteCombatsFilePath);
                var options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
                };
                var temp = JsonSerializer.Deserialize<List<string>>(json, options);
                if (temp != null)
                {
                    _favoriteCombats = temp;
                }
            }
        }

        public bool AddUserCreatureCustomField(DCCreatureCustomField customField)
        {
            var field = GetUserCreatureCustomFieldByName(customField.Title);
            if (field == null)
            {
                _userCreatureCustomFields.Add(customField);
                return true;
            }
            return false;
        }

        public void RemoveUserCreatureCustomFieldByName(string customFieldName)
        {
            var field = GetUserCreatureCustomFieldByName(customFieldName);
            if (field != null)
            {
                _userCreatureCustomFields.Remove(field);
            }
        }

        public List<DCCreatureCustomField> GetUserCreatureCustomFields()
        {
            return _userCreatureCustomFields;
        }

        public DCCreatureCustomField? GetUserCreatureCustomFieldByName(string fieldName)
        {
            return _userCreatureCustomFields.Find(x => x.Title == fieldName);
        }

        private static string userCreatureCustomFieldsFilePath = Path.Combine(FileSystem.AppDataDirectory, "userCreatureCustomFields.json");

        public async Task SaveUserCreatureCustomFieldsListAsync()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
            };
            string json = JsonSerializer.Serialize(_userCreatureCustomFields, options);
            await File.WriteAllTextAsync(userCreatureCustomFieldsFilePath, json);
            Debug.WriteLine($"Write file to {userCreatureCustomFieldsFilePath}");
        }

        public async Task LoadUserCreatureCustomFieldsListAsync()
        {
            if (File.Exists(userCreatureCustomFieldsFilePath))
            {
                Debug.WriteLine("File exists");
                string json = await File.ReadAllTextAsync(userCreatureCustomFieldsFilePath);
                var options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
                };
                var temp = JsonSerializer.Deserialize<List<DCCreatureCustomField>>(json, options);
                if (temp != null)
                {
                    _userCreatureCustomFields = temp;
                }
            }
        }
    }
}

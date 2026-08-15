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
using System.Text.Json;
using System.Diagnostics;
using static MudBlazor.FilterOperator;
using System.Reflection;
using System.Net.Http.Headers;
using System.Globalization;

namespace DiceCombats
{
    public class DiceCombatsService
    {
        private List<DCCreature> _CreatureList = new List<DCCreature>();
        private List<DCCombat> _CombatsList = new List<DCCombat>();
        private List<string> _favoriteCombats = new List<string>();
        private List<DCCreatureCustomField> _userCreatureCustomFields = new List<DCCreatureCustomField>();
        private List<DCPlayerGroup> _playerGroups = new List<DCPlayerGroup>();
        private List<DCCombatLogSession> _combatLogs = new List<DCCombatLogSession>();

        public DiceCombatsService()
        {
            _ = LoadCreatureListAsync();
            _ = LoadCombatsListAsync();
            _ = LoadFavoriteCombatsListAsync();
            _ = LoadUserCreatureCustomFieldsListAsync();
            _ = LoadPlayerGroupsListAsync();
            _ = LoadCombatLogsListAsync();
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

            _ = SaveCreatureListAsync(_CreatureList);
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
                Debug.WriteLine($"File {creatureFilePath} exists");
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
            DeleteCombatLogSessions(combat.Id);
        }

        public void SaveCombats()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true // Optional: for better readability
            };
            string json = JsonSerializer.Serialize(_CombatsList, options);
            Debug.WriteLine(json);

            _ = SaveCombatsListAsync(_CombatsList);
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
                Debug.WriteLine($"File {combatsFilePath} exists");
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

        // Campaigns are derived from the combats themselves, so there is no separate
        // list to keep in sync, so renaming/emptying one can never leave a ghost.

        public List<string> GetCampaigns()
        {
            return _CombatsList
                .Where(x => !string.IsNullOrWhiteSpace(x.Campaign))
                .Select(x => x.Campaign)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(x => x, StringComparer.CurrentCulture)
                .ToList();
        }

        public List<DCCombat> GetCombatsOfCampaign(string campaign)
        {
            return _CombatsList.FindAll(x => string.Equals(x.Campaign, campaign, StringComparison.CurrentCultureIgnoreCase));
        }

        public void SetCombatCampaign(DCCombat combat, string campaign)
        {
            combat.Campaign = (campaign ?? string.Empty).Trim();
            SaveCombats();
        }

        /// <summary>
        /// Renames a campaign by moving every combat that carries the old name.
        /// An empty new name simply takes those combats out of any campaign.
        /// </summary>
        public void RenameCampaign(string oldName, string newName)
        {
            string trimmed = (newName ?? string.Empty).Trim();
            foreach (DCCombat combat in GetCombatsOfCampaign(oldName))
            {
                combat.Campaign = trimmed;
            }
            SaveCombats();
        }

        public void AddCombatToFavoriteByGUID(string guid)
        {
            if (_favoriteCombats.Contains(guid) == false)
            {
                _favoriteCombats.Add(guid);
                _ = SaveFavoriteCombatsListAsync();
            }
        }

        public void RemoveCombatToFavoriteByGUID(string guid)
        {
            if (_favoriteCombats.Contains(guid) == true)
            {
                _favoriteCombats.Remove(guid);
                _ = SaveFavoriteCombatsListAsync();
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

        // Combat logs
        //
        // Kept in their own file rather than on the combat: a combat is exported
        // and imported as a whole, and play history has no business travelling
        // with it. Sessions are trimmed so the file cannot grow without bound.

        private const int MaxLogSessionsPerCombat = 10;

        public List<DCCombatLogSession> GetCombatLogSessions(System.Guid combatId)
        {
            return _combatLogs
                .FindAll(x => x.CombatId == combatId)
                .OrderByDescending(x => x.StartedAt)
                .ToList();
        }

        public void AddCombatLogSession(DCCombatLogSession session)
        {
            _combatLogs.Add(session);

            var forCombat = _combatLogs.FindAll(x => x.CombatId == session.CombatId).OrderByDescending(x => x.StartedAt).ToList();
            foreach (var old in forCombat.Skip(MaxLogSessionsPerCombat))
            {
                _combatLogs.Remove(old);
            }

            SaveCombatLogs();
        }

        public void DeleteCombatLogSession(DCCombatLogSession session)
        {
            _combatLogs.Remove(session);
            SaveCombatLogs();
        }

        public void DeleteCombatLogSessions(System.Guid combatId)
        {
            if (_combatLogs.RemoveAll(x => x.CombatId == combatId) > 0)
            {
                SaveCombatLogs();
            }
        }

        private static string combatLogsFilePath = Path.Combine(FileSystem.AppDataDirectory, "combatLogs.json");

        /// <summary>
        /// Written synchronously: a session is stored when the fight ends, which
        /// is often the moment the app is closing, and a background write would
        /// not get the chance to finish.
        /// </summary>
        public void SaveCombatLogs()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
            };
            string json = JsonSerializer.Serialize(_combatLogs, options);
            File.WriteAllText(combatLogsFilePath, json);
            Debug.WriteLine($"Write file to {combatLogsFilePath}");
        }

        public async Task LoadCombatLogsListAsync()
        {
            if (File.Exists(combatLogsFilePath))
            {
                Debug.WriteLine($"File {combatLogsFilePath} exists");
                string json = await File.ReadAllTextAsync(combatLogsFilePath);
                var options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
                };
                var temp = JsonSerializer.Deserialize<List<DCCombatLogSession>>(json, options);
                if (temp != null)
                {
                    _combatLogs = temp;
                }
            }
        }

        // Player groups

        public List<DCPlayerGroup> GetPlayerGroups() { return _playerGroups; }

        public void AddPlayerGroup(DCPlayerGroup group)
        {
            _playerGroups.Add(group);
            _ = SavePlayerGroupsListAsync();
        }

        public void DeletePlayerGroup(DCPlayerGroup group)
        {
            _playerGroups.Remove(group);
            _ = SavePlayerGroupsListAsync();
        }

        public void SavePlayerGroups()
        {
            _ = SavePlayerGroupsListAsync();
        }

        /// <summary>
        /// Resolves a group to the creatures that still exist, in the group's own
        /// order. Members deleted from the bestiary meanwhile are simply skipped.
        /// </summary>
        public List<DCCreature> GetPlayerGroupMembers(DCPlayerGroup group)
        {
            return group.PlayerIds
                .Select(id => _CreatureList.Find(x => x.Id == id))
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();
        }

        private static string playerGroupsFilePath = Path.Combine(FileSystem.AppDataDirectory, "playerGroups.json");

        public async Task SavePlayerGroupsListAsync()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
            };
            string json = JsonSerializer.Serialize(_playerGroups, options);
            await File.WriteAllTextAsync(playerGroupsFilePath, json);
            Debug.WriteLine($"Write file to {playerGroupsFilePath}");
        }

        public async Task LoadPlayerGroupsListAsync()
        {
            if (File.Exists(playerGroupsFilePath))
            {
                Debug.WriteLine($"File {playerGroupsFilePath} exists");
                string json = await File.ReadAllTextAsync(playerGroupsFilePath);
                var options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
                };
                var temp = JsonSerializer.Deserialize<List<DCPlayerGroup>>(json, options);
                if (temp != null)
                {
                    _playerGroups = temp;
                }
            }
        }

        // Generic management

        public string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            if (version != null)
            {
                return version.ToString().Substring(0, version.ToString().LastIndexOf("."));
            }

            return "0.0.0";
        }

        public async Task<GitHubRelease?> CheckUpdate()
        {
            HttpClient _httpClient = new HttpClient();

            string owner = "YannCharbon";
            string repo = "DiceCombats";

            string? token = null;

            string currentVersion = GetVersion();

            try
            {
                // Set GitHub API base URL
                string url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

                // Set User-Agent header (required by GitHub API)
                _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("DiceCombats/1.0");

                // Add token for authentication if provided
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                // Make a GET request to fetch the latest release
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error fetching release: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (release != null)
                {
                    // Parse the current and remote version strings
                    if (Version.TryParse(currentVersion, out var localVersion) && Version.TryParse(release.tag_name.TrimStart('v'), out var remoteVersion))
                    {
                        if (remoteVersion > localVersion)
                        {
                            Debug.WriteLine($"More recent release available: {release.tag_name}");
                            return release;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to parse version strings.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return null; // No new release or error occurred
        }

        public bool IsCurrentCulture(string cultureCode)
        {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == cultureCode;
        }
    }

    public class GitHubRelease
    {
        public string tag_name { get; set; } = "";
        public string html_url { get; set; } = "";
    }
}

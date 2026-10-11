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
        private List<string> _pinnedCombats = new List<string>();
        private List<DCCreatureCustomField> _userCreatureCustomFields = new List<DCCreatureCustomField>();
        private List<DCPlayerGroup> _playerGroups = new List<DCPlayerGroup>();
        private List<DCCombatLogSession> _combatLogs = new List<DCCombatLogSession>();

        public DiceCombatsService()
        {
            _ = LoadCreatureListAsync();
            _ = LoadCombatsListAsync();
            _ = LoadPinnedCombatsListAsync();
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

        public void AddCombatToPinnedByGUID(string guid)
        {
            if (_pinnedCombats.Contains(guid) == false)
            {
                _pinnedCombats.Add(guid);
                _ = SavePinnedCombatsListAsync();
            }
        }

        public void RemoveCombatFromPinnedByGUID(string guid)
        {
            if (_pinnedCombats.Contains(guid) == true)
            {
                _pinnedCombats.Remove(guid);
                _ = SavePinnedCombatsListAsync();
            }
        }

        public bool IsCombatPinned(string guid)
        {
            return _pinnedCombats.Contains(guid);
        }

        public List<string> GetPinnedCombatsGuids()
        {
            foreach (var guid in _pinnedCombats.ToArray())
            if (GetCombatFromGUID(guid) == null)
            {
                // Update the list if anything has changed
                RemoveCombatFromPinnedByGUID(guid);
            }
            return _pinnedCombats;
        }

        private static string pinnedCombatsFilePath = Path.Combine(FileSystem.AppDataDirectory, "pinnedCombats.json");

        // Pinned combats used to be called "favorites" and were stored under this
        // name. It is only ever read, once, by LoadPinnedCombatsListAsync when the
        // new file is missing, so that users who pinned combats before the rename
        // keep them. The legacy file is deliberately left on disk: if the user ever
        // downgrades, it is their only copy of the list.
        private static string legacyFavoriteCombatsFilePath = Path.Combine(FileSystem.AppDataDirectory, "favoritesCombats.json");

        public async Task SavePinnedCombatsListAsync()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
            };
            string json = JsonSerializer.Serialize(_pinnedCombats, options);
            await File.WriteAllTextAsync(pinnedCombatsFilePath, json);
            Debug.WriteLine($"Write file to {pinnedCombatsFilePath}");
        }

        public async Task LoadPinnedCombatsListAsync()
        {
            bool fromLegacyFile = false;
            string? path = null;

            if (File.Exists(pinnedCombatsFilePath))
            {
                Debug.WriteLine("File exists");
                path = pinnedCombatsFilePath;
            }
            else if (File.Exists(legacyFavoriteCombatsFilePath))
            {
                // Migration: the list was saved before pinned combats were called
                // pinned. Read it from the old name and write it to the new one.
                Debug.WriteLine($"Legacy file exists, migrating {legacyFavoriteCombatsFilePath} to {pinnedCombatsFilePath}");
                path = legacyFavoriteCombatsFilePath;
                fromLegacyFile = true;
            }

            if (path != null)
            {
                string json = await File.ReadAllTextAsync(path);
                var options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 15 * 1024 * 1024 // 15MiB
                };
                var temp = JsonSerializer.Deserialize<List<string>>(json, options);
                if (temp != null)
                {
                    _pinnedCombats = temp;
                }

                if (fromLegacyFile)
                {
                    // Write straight away so the new path becomes the source of
                    // truth even if the app is killed before the next save. The
                    // legacy file is left untouched on purpose.
                    await SavePinnedCombatsListAsync();
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

        /// <summary>
        /// The version this build reports. The release workflow rewrites
        /// &lt;AssemblyVersion&gt; in the csproj from the tag it is building, so this is
        /// the tag for a published build and the csproj's own value for a local one
        /// - which is why that value is kept in step with the last release rather
        /// than left at 0.0.0, as it was.
        /// </summary>
        public string GetVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            // Major.Minor.Build spelled out: the old code cut everything after the
            // last dot of ToString(), which turns a three-part version into "0.0".
            return version == null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
        }

        // ── checking for a newer release ───────────────────────────────────────
        // One client for the process. A fresh HttpClient per call leaks a socket
        // per call, and this used to run on every visit to the home page and to
        // the settings page.
        private static readonly HttpClient UpdateHttpClient = CreateUpdateHttpClient();

        private static HttpClient CreateUpdateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            // The GitHub API rejects a request with no User-Agent.
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("DiceCombats");
            return client;
        }

        /// How long a successful answer is trusted before the network is asked
        /// again. GitHub allows 60 unauthenticated requests an hour per IP and the
        /// app used to spend one on every page visit, so a few minutes of browsing
        /// exhausted the quota - after which every reply is 403.
        private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(6);

        private const string PrefUpdateCheckedUtc = "UpdateLastCheckUtc";
        private const string PrefUpdateLatestTag = "UpdateLatestTag";
        private const string PrefUpdateLatestUrl = "UpdateLatestUrl";

        private DCUpdateStatus? _updateStatus;

        /// <summary>
        /// What the last check concluded, without touching the network. Null only
        /// before <see cref="CheckUpdateAsync"/> has run once in this process.
        /// </summary>
        public DCUpdateStatus? LastUpdateStatus => _updateStatus;

        /// <summary>
        /// Asks GitHub for the latest release, at most once every
        /// <see cref="UpdateCheckInterval"/> unless <paramref name="force"/> says
        /// otherwise, and never downgrades what is already known: a check that
        /// fails reports <see cref="DCUpdateCheckState.CheckFailed"/> while keeping
        /// the release it last saw. Reporting a failure as "up to date" is what
        /// made a detected update vanish from the settings minutes after it
        /// appeared.
        /// </summary>
        public async Task<DCUpdateStatus> CheckUpdateAsync(bool force = false)
        {
            var known = _updateStatus ?? LoadPersistedUpdateStatus();

            if (!force
                && known.State != DCUpdateCheckState.CheckFailed
                && known.CheckedUtc is System.DateTime last
                && System.DateTime.UtcNow - last < UpdateCheckInterval)
            {
                _updateStatus = known;
                return known;
            }

            GitHubRelease? release = null;
            try
            {
                var response = await UpdateHttpClient.GetAsync(
                    $"https://api.github.com/repos/YannCharbon/DiceCombats/releases/latest");

                if (response.IsSuccessStatusCode)
                {
                    release = JsonSerializer.Deserialize<GitHubRelease>(
                        await response.Content.ReadAsStringAsync(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    Debug.WriteLine($"Update check refused: {(int)response.StatusCode} {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
            }

            if (release == null || string.IsNullOrWhiteSpace(release.tag_name))
            {
                _updateStatus = known with { State = DCUpdateCheckState.CheckFailed };
                return _updateStatus;
            }

            Preferences.Set(PrefUpdateLatestTag, release.tag_name);
            Preferences.Set(PrefUpdateLatestUrl, release.html_url ?? string.Empty);
            Preferences.Set(PrefUpdateCheckedUtc, System.DateTime.UtcNow.Ticks);

            _updateStatus = new DCUpdateStatus(
                IsNewerThanInstalled(release.tag_name)
                    ? DCUpdateCheckState.UpdateAvailable
                    : DCUpdateCheckState.UpToDate,
                release,
                System.DateTime.UtcNow);

            return _updateStatus;
        }

        /// The answer from a previous run, so a cold start with no network still
        /// shows what was last true instead of claiming the app is up to date.
        private DCUpdateStatus LoadPersistedUpdateStatus()
        {
            string tag = Preferences.Get(PrefUpdateLatestTag, string.Empty);
            long ticks = Preferences.Get(PrefUpdateCheckedUtc, 0L);

            if (string.IsNullOrWhiteSpace(tag) || ticks == 0)
            {
                return new DCUpdateStatus(DCUpdateCheckState.NotChecked, null, null);
            }

            var release = new GitHubRelease
            {
                tag_name = tag,
                html_url = Preferences.Get(PrefUpdateLatestUrl, string.Empty),
            };

            return new DCUpdateStatus(
                IsNewerThanInstalled(tag)
                    ? DCUpdateCheckState.UpdateAvailable
                    : DCUpdateCheckState.UpToDate,
                release,
                new System.DateTime(ticks, System.DateTimeKind.Utc));
        }

        /// Compares the tag against this build. A tag that will not parse is
        /// treated as newer only when it differs from the installed version, so a
        /// release named something unexpected is surfaced rather than swallowed.
        private bool IsNewerThanInstalled(string tag)
        {
            string remote = tag.TrimStart('v', 'V');
            string installed = GetVersion();

            if (Version.TryParse(remote, out var remoteVersion)
                && Version.TryParse(installed, out var installedVersion))
            {
                return remoteVersion > installedVersion;
            }

            return !string.Equals(remote, installed, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsCurrentCulture(string cultureCode)
        {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == cultureCode;
        }
    }

    /// <summary>How a check for a newer release turned out.</summary>
    public enum DCUpdateCheckState
    {
        /// Nothing has been asked yet, in this run or any previous one.
        NotChecked,
        UpToDate,
        UpdateAvailable,
        /// The network or the GitHub API refused. Deliberately distinct from
        /// UpToDate so a rate-limited reply cannot erase a real answer.
        CheckFailed,
    }

    /// <summary>The conclusion, the release it is about, and when it was reached.</summary>
    public sealed record DCUpdateStatus(DCUpdateCheckState State, GitHubRelease? Release, System.DateTime? CheckedUtc);

    public class GitHubRelease
    {
        public string tag_name { get; set; } = "";
        public string html_url { get; set; } = "";
    }
}

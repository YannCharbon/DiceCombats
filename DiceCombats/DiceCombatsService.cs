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
using DiceCombats.Storage;

namespace DiceCombats
{
    public class DiceCombatsService
    {
        // The data files, by name: SafeFileStore turns them into paths inside
        // the data folder that DataLocation points at.
        public const string CreaturesFileName = "creatures.json";
        public const string CombatsFileName = "combats.json";
        public const string PinnedCombatsFileName = "pinnedCombats.json";
        public const string UserCreatureCustomFieldsFileName = "userCreatureCustomFields.json";
        public const string PlayerGroupsFileName = "playerGroups.json";
        public const string CombatLogsFileName = "combatLogs.json";

        // Pinned combats used to be called "favorites" and were stored under this
        // name. It is only ever read, once, by LoadPinnedCombatsListAsync when the
        // new file is missing, so that users who pinned combats before the rename
        // keep them. The legacy file is deliberately left on disk: if the user ever
        // downgrades, it is their only copy of the list.
        public const string LegacyFavoriteCombatsFileName = "favoritesCombats.json";

        // The same options the files have always been written with, so that a
        // file nobody changed is recognised as unchanged and left alone.
        private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions();
        private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions { WriteIndented = true };

        // Every save passes overwriteExternalChange: true. A file changed on
        // another computer is overwritten, as it always has been, until the app
        // can ask the user whether to reload or overwrite it.

        private readonly SafeFileStore _store;
        private readonly ILocalPreferences _localPreferences;

        private List<DCCreature> _CreatureList = new List<DCCreature>();
        private List<DCCombat> _CombatsList = new List<DCCombat>();
        private List<string> _pinnedCombats = new List<string>();
        private List<DCCreatureCustomField> _userCreatureCustomFields = new List<DCCreatureCustomField>();
        private List<DCPlayerGroup> _playerGroups = new List<DCPlayerGroup>();
        private List<DCCombatLogSession> _combatLogs = new List<DCCombatLogSession>();

        public DiceCombatsService(SafeFileStore store, ILocalPreferences localPreferences)
        {
            _store = store;
            _localPreferences = localPreferences;
        }

        /// <summary>
        /// Loads every data file, each on its own: a file that cannot be read
        /// leaves its list empty, is never saved over, and does not stop the
        /// others from loading. Called once at startup, before any page.
        /// </summary>
        /// <summary>
        /// False until the data has been read. Until then nothing is saved: the
        /// lists are empty, and the data folder may not even be reachable yet.
        /// </summary>
        public bool IsInitialized { get; private set; }

        public async Task InitializeAsync()
        {
            await LoadGuardedAsync(CreaturesFileName, LoadCreatureListAsync);
            await LoadGuardedAsync(CombatsFileName, LoadCombatsListAsync);
            await LoadGuardedAsync(PinnedCombatsFileName, LoadPinnedCombatsListAsync);
            await LoadGuardedAsync(UserCreatureCustomFieldsFileName, LoadUserCreatureCustomFieldsListAsync);
            await LoadGuardedAsync(PlayerGroupsFileName, LoadPlayerGroupsListAsync);
            await LoadGuardedAsync(CombatLogsFileName, LoadCombatLogsListAsync);
            IsInitialized = true;
        }

        // Anything that goes wrong while one file is taken in blocks that file
        // and lets the next one load.
        private async Task LoadGuardedAsync(string fileName, Func<Task> load)
        {
            try
            {
                await load();
            }
            catch (Exception ex)
            {
                _store.Block(fileName, ex.Message);
            }
        }

        /// <summary>
        /// Reads the files again, after a restore. Each list is replaced only
        /// once its file has been read (a missing file gives an empty list); a
        /// file that cannot be read keeps the list as it was, and stays blocked.
        /// </summary>
        public Task ReloadAsync()
        {
            return InitializeAsync();
        }

        /// <summary>
        /// Starts a save of every file. Each is written only if it changed; the
        /// returned task completes when all of them are on disk.
        /// </summary>
        public Task SaveAllAsync()
        {
            return Task.WhenAll(
                SaveCreaturesAsync(),
                SaveCombatsAsync(),
                SavePinnedCombatsListAsync(),
                SaveUserCreatureCustomFieldsListAsync(),
                SavePlayerGroupsListAsync(),
                SaveCombatLogsAsync());
        }

        /// The list read from a file, an empty list when the file is missing,
        /// or null when it cannot be read (the list in memory is then kept).
        private async Task<List<T>?> LoadListAsync<T>(string fileName)
        {
            var result = await _store.LoadJsonAsync<List<T>>(fileName, ReadOptions);
            Debug.WriteLine($"Load {fileName}: {result.Outcome} {result.Error}");
            return result.Outcome switch
            {
                LoadOutcome.Loaded => result.Value,
                LoadOutcome.Missing => new List<T>(),
                _ => null,
            };
        }

        public DCCreature? GetCreatureFromGUID(string guid)
        {
            return _CreatureList.Find(x => x.Id.ToString() == guid);
        }

        public List<DCCreature> GetCreatureList() { return _CreatureList; }

        /// <summary>Adds to the list in memory; the caller saves with <see cref="SaveCreatures"/>.</summary>
        public void AddCreature(DCCreature creature)
        {
            _CreatureList.Add(creature);
        }

        public void DeleteCreature(DCCreature creature)
        {
            _CreatureList.Remove(creature);
        }

        /// <summary>Saves the creatures as they are now. The write itself finishes in the background.</summary>
        public void SaveCreatures()
        {
            _ = SaveCreaturesAsync();
        }

        public Task<SaveOutcome> SaveCreaturesAsync()
        {
            if (!IsInitialized) return Task.FromResult(SaveOutcome.Blocked);
            return _store.SaveJsonAsync(CreaturesFileName, _CreatureList, WriteOptions, overwriteExternalChange: true);
        }

        public async Task LoadCreatureListAsync()
        {
            var temp = await LoadListAsync<DCCreature>(CreaturesFileName);
            if (temp != null)
            {
                _CreatureList = temp;
            }
        }


        // Combats

        public DCCombat? GetCombatFromGUID(string guid)
        {
            return _CombatsList.Find(x => x.Id.ToString() == guid);
        }

        public List<DCCombat> GetCombatList() { return _CombatsList; }

        /// <summary>Adds to the list in memory; the caller saves with <see cref="SaveCombats"/>.</summary>
        public void AddCombat(DCCombat combat)
        {
            _CombatsList.Add(combat);
        }

        public void DeleteCombat(DCCombat combat)
        {
            _CombatsList.Remove(combat);
            DeleteCombatLogSessions(combat.Id);
            RemoveCombatFromPinnedByGUID(combat.Id.ToString());
        }

        /// <summary>Saves the combats as they are now. The write itself finishes in the background.</summary>
        public void SaveCombats()
        {
            _ = SaveCombatsAsync();
        }

        public Task<SaveOutcome> SaveCombatsAsync()
        {
            if (!IsInitialized) return Task.FromResult(SaveOutcome.Blocked);
            return _store.SaveJsonAsync(CombatsFileName, _CombatsList, WriteOptions, overwriteExternalChange: true);
        }

        public async Task LoadCombatsListAsync()
        {
            var temp = await LoadListAsync<DCCombat>(CombatsFileName);
            if (temp != null)
            {
                _CombatsList = temp;
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

        /// <summary>
        /// The pinned combats that still exist. A getter never saves: a pin
        /// whose combat is missing is only left out here, since the combats
        /// may simply have failed to load. Deleting a combat unpins it.
        /// </summary>
        public List<string> GetPinnedCombatsGuids()
        {
            return _pinnedCombats.Where(guid => GetCombatFromGUID(guid) != null).ToList();
        }

        public Task<SaveOutcome> SavePinnedCombatsListAsync()
        {
            if (!IsInitialized) return Task.FromResult(SaveOutcome.Blocked);
            return _store.SaveJsonAsync(PinnedCombatsFileName, _pinnedCombats, WriteOptions, overwriteExternalChange: true);
        }

        public async Task LoadPinnedCombatsListAsync()
        {
            var result = await _store.LoadJsonAsync<List<string>>(PinnedCombatsFileName, ReadOptions);
            Debug.WriteLine($"Load {PinnedCombatsFileName}: {result.Outcome} {result.Error}");

            if (result.Outcome == LoadOutcome.Missing)
            {
                // Migration: the list was saved before pinned combats were called
                // pinned. Read it from the old name and write it to the new one.
                // A pinned file that exists but cannot be read is not replaced.
                var legacyResult = await _store.LoadJsonAsync<List<string>>(LegacyFavoriteCombatsFileName, ReadOptions);
                var legacy = legacyResult.Outcome == LoadOutcome.Loaded ? legacyResult.Value : null;
                if (legacyResult.Outcome == LoadOutcome.Failed)
                {
                    // The pins exist but cannot be read: an empty pinned file
                    // must not take their place.
                    _store.Block(PinnedCombatsFileName, $"{LegacyFavoriteCombatsFileName} could not be read: {legacyResult.Error}");
                }
                else if (legacy != null)
                {
                    Debug.WriteLine($"Migrating {LegacyFavoriteCombatsFileName} to {PinnedCombatsFileName}");
                    _pinnedCombats = legacy;

                    // Write straight away so the new file becomes the source of
                    // truth even if the app is killed before the next save. The
                    // legacy file is left untouched on purpose. Written through
                    // the store directly: the service saves nothing while it is
                    // still loading.
                    await _store.SaveJsonAsync(PinnedCombatsFileName, _pinnedCombats, WriteOptions, overwriteExternalChange: true);
                }
                else
                {
                    _pinnedCombats = new List<string>();
                }
            }
            else if (result.Value != null)
            {
                _pinnedCombats = result.Value;
            }
        }

        public bool AddUserCreatureCustomField(DCCreatureCustomField customField)
        {
            var field = GetUserCreatureCustomFieldByName(customField.Title);
            if (field == null)
            {
                _userCreatureCustomFields.Add(customField);
                _ = SaveUserCreatureCustomFieldsListAsync();
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
                _ = SaveUserCreatureCustomFieldsListAsync();
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

        public Task<SaveOutcome> SaveUserCreatureCustomFieldsListAsync()
        {
            if (!IsInitialized) return Task.FromResult(SaveOutcome.Blocked);
            return _store.SaveJsonAsync(UserCreatureCustomFieldsFileName, _userCreatureCustomFields, WriteOptions, overwriteExternalChange: true);
        }

        public async Task LoadUserCreatureCustomFieldsListAsync()
        {
            var temp = await LoadListAsync<DCCreatureCustomField>(UserCreatureCustomFieldsFileName);
            if (temp != null)
            {
                _userCreatureCustomFields = temp;
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

        /// <summary>
        /// A session is stored when the fight ends, which is often the moment
        /// the app is closing: the close handlers wait for this write through
        /// SafeFileStore.FlushAsync.
        /// </summary>
        public void SaveCombatLogs()
        {
            _ = SaveCombatLogsAsync();
        }

        public Task<SaveOutcome> SaveCombatLogsAsync()
        {
            if (!IsInitialized) return Task.FromResult(SaveOutcome.Blocked);
            return _store.SaveJsonAsync(CombatLogsFileName, _combatLogs, WriteOptions, overwriteExternalChange: true);
        }

        public async Task LoadCombatLogsListAsync()
        {
            var temp = await LoadListAsync<DCCombatLogSession>(CombatLogsFileName);
            if (temp != null)
            {
                _combatLogs = temp;
            }
        }

        /// <summary>
        /// Adds the combat logs and pinned combats of another library, when two
        /// data folders are merged: sessions by id, with the usual limit per
        /// combat; pins of combats that exist, with the stale ones dropped.
        /// </summary>
        public async Task MergeLogsAndPinsAsync(IEnumerable<DCCombatLogSession> sessions, IEnumerable<string> pins)
        {
            foreach (var session in sessions)
            {
                if (!_combatLogs.Exists(x => x.Id == session.Id)) _combatLogs.Add(session);
            }
            foreach (var group in _combatLogs.GroupBy(x => x.CombatId).ToList())
            {
                foreach (var old in group.OrderByDescending(x => x.StartedAt).Skip(MaxLogSessionsPerCombat))
                {
                    _combatLogs.Remove(old);
                }
            }

            foreach (string pin in pins)
            {
                if (!_pinnedCombats.Contains(pin)) _pinnedCombats.Add(pin);
            }
            _pinnedCombats.RemoveAll(guid => GetCombatFromGUID(guid) == null);

            await SaveCombatLogsAsync();
            await SavePinnedCombatsListAsync();
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

        public Task<SaveOutcome> SavePlayerGroupsListAsync()
        {
            if (!IsInitialized) return Task.FromResult(SaveOutcome.Blocked);
            return _store.SaveJsonAsync(PlayerGroupsFileName, _playerGroups, WriteOptions, overwriteExternalChange: true);
        }

        public async Task LoadPlayerGroupsListAsync()
        {
            var temp = await LoadListAsync<DCPlayerGroup>(PlayerGroupsFileName);
            if (temp != null)
            {
                _playerGroups = temp;
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

            _localPreferences.Set(PrefUpdateLatestTag, release.tag_name);
            _localPreferences.Set(PrefUpdateLatestUrl, release.html_url ?? string.Empty);
            _localPreferences.Set(PrefUpdateCheckedUtc, System.DateTime.UtcNow.Ticks);

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
            string tag = _localPreferences.Get(PrefUpdateLatestTag, string.Empty);
            long ticks = _localPreferences.Get(PrefUpdateCheckedUtc, 0L);

            if (string.IsNullOrWhiteSpace(tag) || ticks == 0)
            {
                return new DCUpdateStatus(DCUpdateCheckState.NotChecked, null, null);
            }

            var release = new GitHubRelease
            {
                tag_name = tag,
                html_url = _localPreferences.Get(PrefUpdateLatestUrl, string.Empty),
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

/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Diagnostics;
using System.Text.Json;
using DiceCombats.Automations;
using DiceCombats.Portability;

namespace DiceCombats.Storage;

/// <summary>
/// The data folder of DiceCombats (spec §5.4, §5.6): loading at startup once
/// the folder can be used, the marker, moving to another folder, using or
/// replacing or merging with a folder that already holds DiceCombats data,
/// and the old copy left behind. Windows only for the moves; the UI asks.
/// </summary>
public sealed class DataFolderService
{
    /// <summary>The local preference that remembers where the data was before a move, until the old copy is deleted.</summary>
    public const string PreviousRootKey = "PreviousDataRootPath";

    /// <summary>The preferences that move into settings.json (spec §6).</summary>
    public static readonly IReadOnlyList<string> MovedPreferenceKeys = new[] { "Language", "AssistanceField", "AssistanceAutomations" };

    /// <summary>What a move copies: the data files, the backups and the hand-made Backup/ folder.</summary>
    public static readonly IReadOnlyList<string> MovedEntries = BackupService.DataFileNames.Concat(new[] { "Backups", "Backup" }).ToList();

    private readonly DataLocation _location;
    private readonly SafeFileStore _store;
    private readonly DiceCombatsService _combats;
    private readonly AutomationConfigurationService _automations;
    private readonly CombatLogService _combatLog;
    private readonly BackupService _backups;
    private readonly AppSettings _settings;
    private readonly ILocalPreferences _localPreferences;
    private readonly StorageNotices _notices;
    private readonly DataFolderWatcher _watcher;
    private readonly DataFolderLock _folderLock;

    /// <summary>The data files watched for changes made on another computer (the legacy pinned list is never read, so not watched).</summary>
    public static readonly IReadOnlyList<string> WatchedFiles =
        BackupService.DataFileNames.Where(n => n != DiceCombatsService.LegacyFavoriteCombatsFileName).ToList();

    public DataFolderService(DataLocation location, SafeFileStore store, DiceCombatsService combats, AutomationConfigurationService automations,
                             CombatLogService combatLog, BackupService backups, AppSettings settings, ILocalPreferences localPreferences,
                             StorageNotices notices, DataFolderWatcher? watcher = null)
    {
        _watcher = watcher ?? new DataFolderWatcher(store, WatchedFiles);
        _folderLock = new DataFolderLock(store, () => location.Root, location.AppName, location.MachineName, combats.GetVersion());
        _watcher.ChangedElsewhere += _ => ChangedElsewhere?.Invoke();
        combats.ChangedOnDisk += notices.ReportChangedOnDisk;
        _location = location;
        _store = store;
        _combats = combats;
        _automations = automations;
        _combatLog = combatLog;
        _backups = backups;
        _settings = settings;
        _localPreferences = localPreferences;
        _notices = notices;
    }

    /// <summary>True once the data has been read; until then nothing is saved.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Raised when IsLoaded changes, for the startup screen.</summary>
    public event Action? LoadedChanged;

    public DataLocation Location => _location;

    /// <summary>
    /// Reads everything, once the folder can be used: the settings (with the
    /// one-time copy of the old preferences), the data files, the marker, and
    /// the automatic backup of the day, in the background.
    /// </summary>
    public async Task LoadAsync()
    {
        if (IsLoaded) return;

        // Before anything is read: what a synchronisation service left behind
        // (spec §5.7). Reading a file only in the cloud would download it.
        _notices.ReportCloudFiles(CloudFiles.FindConflictCopies(_location.Root, BackupService.DataFileNames),
                                  CloudFiles.FindCloudOnly(_location.Root, BackupService.DataFileNames));
        var unsaved = UnsavedCopies.Find(_location.Root, BackupService.DataFileNames);
        _notices.ReportUnsavedCopies(unsaved.Where(IsImportableUnsavedCopy).ToList(),
                                     unsaved.Where(n => !IsImportableUnsavedCopy(n)).ToList());

        if (!_settings.IsInitialized)
        {
            await _settings.InitializeAsync(_localPreferences, MovedPreferenceKeys);
        }
        await _combats.InitializeAsync();
        await _automations.InitializeAsync();
        _notices.ReportLoadFailures();
        await WriteMarkerAsync();

        // Windows only (spec §5.5): the lock says this computer has the folder
        // open, and the watcher notices what another computer changes in it.
        if (OperatingSystem.IsWindows())
        {
            await _folderLock.AcquireAsync();
            _watcher.Start(_location.Root);
        }

        IsLoaded = true;
        LoadedChanged?.Invoke();

        // The first start of the day on this machine makes a backup, once the
        // saves the load itself started are on disk. Not after a file failed to
        // load: the backups kept would slowly become copies of the broken state.
        if (_store.BlockedFiles.Count == 0)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _store.FlushAsync();
                    await _backups.AutoBackupIfDueAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Automatic backup failed: {ex}");
                }
            });
        }
    }

    /// <summary>The marker of the data folder: created with a new LibraryId the first time, its last writer updated each time.</summary>
    public async Task WriteMarkerAsync()
    {
        try
        {
            var current = DataFolderMarker.Read(_location.Root);
            var marker = DataFolderMarker.Create(_location.AppName, current?.LibraryId ?? Guid.NewGuid(), _location.MachineName, _combats.GetVersion());
            await _store.SaveTextAsync(DataFolderMarker.FileName, marker.ToJson(), overwriteExternalChange: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"The marker could not be written: {ex.Message}");
        }
    }

    // ── several computers ──────────────────────────────────────────────

    /// <summary>Raised when a data file was changed elsewhere (the banner shows); see <see cref="ChangedFiles"/>.</summary>
    public event Action? ChangedElsewhere;

    /// <summary>The data files changed on another computer since they were read here.</summary>
    public IReadOnlyList<string> ChangedFiles => _watcher.Changed;

    public DataFolderWatcher Watcher => _watcher;

    /// <summary>
    /// "Reload": reads every data file again, dropping what is in memory. Only
    /// when the user asks: the app never reloads by itself, in a combat or not.
    /// The caller rebuilds the pages.
    /// </summary>
    public async Task ReloadAllAsync()
    {
        using (_watcher.Pause())
        {
            await _combats.ReloadAsync();
            await _automations.ReloadAsync();
            await _settings.ReloadAsync();
        }
        _watcher.Clear();
        _notices.ReportLoadFailures();
        ChangedElsewhere?.Invoke();
    }

    /// <summary>"Overwrite": saves one file over what another computer wrote in it.</summary>
    public async Task OverwriteAsync(string fileName)
    {
        if (fileName == AutomationConfigurationService.AutomationsFileName)
        {
            await _automations.SaveAsync(overwriteExternalChange: true);
        }
        else
        {
            await _combats.OverwriteAsync(fileName);
        }
        _watcher.Clear(new[] { fileName });
        ChangedElsewhere?.Invoke();
    }

    // ── changes that could not be saved ────────────────────────────────

    /// <summary>A kept list of creatures or combats: Import from a file reads it as an older export.</summary>
    public static bool IsImportableUnsavedCopy(string name) =>
        name.StartsWith(Path.GetFileNameWithoutExtension(DiceCombatsService.CreaturesFileName) + ".", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith(Path.GetFileNameWithoutExtension(DiceCombatsService.CombatsFileName) + ".", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// When the app closes: each file whose last save was refused because
    /// another computer had changed it (a "Reload or Overwrite" choice still
    /// open, or a refused save on close) gets the app's version written next
    /// to it by SafeFileStore (UnsavedCopies), so that nothing is lost. The
    /// data file itself is left as the other computer wrote it.
    /// Synchronous: called while the window closes.
    /// </summary>
    public IReadOnlyList<string> KeepUnsavedChanges() =>
        IsLoaded ? _store.WriteUnsavedCopies(_location.MachineName, DateTime.Now) : Array.Empty<string>();

    /// <summary>
    /// At a clean exit, or before a restart: stops the heartbeat and deletes
    /// this session's lock. Synchronous: called while the window closes.
    /// </summary>
    public void ReleaseFolder()
    {
        _watcher.Stop();
        _folderLock.Release();
    }

    // ── moving ──────────────────────────────────────────────────────────

    public string? PreviousRoot
    {
        get
        {
            string value = _localPreferences.Get(PreviousRootKey, string.Empty) ?? string.Empty;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    /// <summary>Checks a folder chosen as the new data folder (spec §5.6). Changes nothing but may create the folder.</summary>
    public MoveTargetResult CheckTarget(string folder)
    {
        long needed = DataFolderMove.SizeOf(_location.Root, MovedEntries);
        var appData = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        };
        var result = DataFolderMove.CheckTarget(folder, _location.Root, _location.DefaultRoot, _location.AppName, needed, appData);

        // The default folder may still hold the data from before a move,
        // without a marker if it was never opened by this version: that is a
        // library, not "some other files" calling for a subfolder.
        if (result.Check == MoveTargetCheck.UseSubfolder && IsDefault(folder)
            && BackupService.DataFileNames.Any(n => File.Exists(Path.Combine(folder, n))))
        {
            if (Directory.Exists(result.Folder) && !Directory.EnumerateFileSystemEntries(result.Folder).Any())
            {
                Directory.Delete(result.Folder);   // the subfolder the check made
            }
            return result with { Check = MoveTargetCheck.ExistingLibrary, Folder = _location.DefaultRoot };
        }
        return result;
    }

    /// <summary>
    /// Moves the data to an empty folder (or the default one): saves and stops
    /// everything, makes a before-move backup, copies and checks, writes the
    /// marker, then points at the new folder. The caller restarts the app.
    /// </summary>
    public async Task MoveAsync(string target, IProgress<(int Done, int Total)>? progress = null)
    {
        await StopAndFlushAsync();
        await _backups.CreateBackupAsync(BackupReason.BeforeMove);
        var blocked = await BlockAllAsync();
        try
        {
            // A copy cut off before (the PC turned off) is resumed: the
            // files already copied with the right size are kept.
            await DataFolderMove.CopyAsync(_location.Root, MovedEntries, target, Array.Empty<string>(), progress, resume: true);
            WriteMarkerTo(target);
        }
        catch
        {
            await UnblockAsync(blocked);
            throw;
        }
        _location.SetPointer(IsDefault(target) ? null : target);
        _localPreferences.Set(PreviousRootKey, _location.Root);
    }

    /// <summary>"Use the folder's data": points at a folder that already holds DiceCombats data. Nothing is copied.</summary>
    public async Task UseExistingAsync(string target)
    {
        await StopAndFlushAsync();
        await BlockAllAsync();
        _location.SetPointer(IsDefault(target) ? null : target);
    }

    /// <summary>
    /// "Replace it with mine": the folder's data is backed up in its own
    /// Backups/, then replaced by this computer's data, which is then used.
    /// </summary>
    public async Task ReplaceExistingAsync(string target, IProgress<(int Done, int Total)>? progress = null)
    {
        await StopAndFlushAsync();
        // Two backups made in the same second: this computer's name must stay
        // free in the folder's Backups/, which this computer's Backups/ joins.
        await _backups.CreateBackupOfFolderAsync(target, BackupReason.BeforeMove);
        await _backups.CreateBackupAsync(BackupReason.BeforeMove, new[] { Path.Combine(target, "Backups") });
        var blocked = await BlockAllAsync();
        try
        {
            await DataFolderMove.CopyAsync(_location.Root, MovedEntries, target, BackupService.DataFileNames, progress, resume: true);
            WriteMarkerTo(target);
        }
        catch
        {
            await UnblockAsync(blocked);
            throw;
        }
        string oldRoot = _location.Root;
        _location.SetPointer(IsDefault(target) ? null : target);
        _localPreferences.Set(PreviousRootKey, oldRoot);
    }

    /// <summary>
    /// The library of another folder, as a bundle: "Merge" imports it into
    /// this computer's data with the import wizard, then replaces the folder's
    /// data with the result.
    /// </summary>
    public static byte[] ReadFolderAsBundle(string folder, string appVersion)
    {
        T Load<T>(string name, JsonSerializerOptions? options = null) where T : new()
        {
            string path = Path.Combine(folder, name);
            return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), options) ?? new T() : new T();
        }

        var automations = Load<AutomationConfiguration>(AutomationConfigurationService.AutomationsFileName, new JsonSerializerOptions { WriteIndented = true });
        var bundle = new DCBundle
        {
            AppVersion = appVersion,
            Roots = { new BundleRoot { Kind = DCBundleKinds.Library } },
            Creatures = Load<List<DCCreature>>(DiceCombatsService.CreaturesFileName),
            Combats = Load<List<DCCombat>>(DiceCombatsService.CombatsFileName),
            PlayerGroups = Load<List<DCPlayerGroup>>(DiceCombatsService.PlayerGroupsFileName),
            FieldTemplates = Load<List<DCCreatureCustomField>>(DiceCombatsService.UserCreatureCustomFieldsFileName),
            ExternalApps = automations.ExternalApps,
            Automations = automations.Bindings,
        };
        return DCBundleSerializer.Write(bundle);
    }

    /// <summary>
    /// Before "Merge" replaces a folder's data: its combat logs and pinned
    /// combats, which an import never carries, are added to this computer's.
    /// </summary>
    public async Task MergeLogsAndPinsFromAsync(string folder)
    {
        var sessions = ReadList<DCCombatLogSession>(folder, DiceCombatsService.CombatLogsFileName);
        string pinsFile = File.Exists(Path.Combine(folder, DiceCombatsService.PinnedCombatsFileName))
            ? DiceCombatsService.PinnedCombatsFileName
            : DiceCombatsService.LegacyFavoriteCombatsFileName;
        await _combats.MergeLogsAndPinsAsync(sessions, ReadList<string>(folder, pinsFile));
        await _store.FlushAsync();
    }

    /// <summary>A folder the check made for nothing (the move was cancelled) is removed, when it is still empty.</summary>
    public static void DiscardTarget(MoveTargetResult check)
    {
        if (check.Check is MoveTargetCheck.Ok or MoveTargetCheck.UseSubfolder
            && Directory.Exists(check.Folder) && !Directory.EnumerateFileSystemEntries(check.Folder).Any())
        {
            try { Directory.Delete(check.Folder); }
            catch (IOException) { }
        }
    }

    private static List<T> ReadList<T>(string folder, string name)
    {
        string path = Path.Combine(folder, name);
        return File.Exists(path) ? JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path)) ?? new List<T>() : new List<T>();
    }

    /// <summary>Deletes the data left in the previous folder after a move, once the user asks.</summary>
    public void DeleteOldCopy()
    {
        string? previous = PreviousRoot;
        if (previous == null) return;
        if (Directory.Exists(previous) && !DataFolderMove.IsSameOrInside(previous, _location.Root))
        {
            DataFolderMove.DeleteOldCopy(previous, MovedEntries, keepFolder: IsDefault(previous));
        }
        _localPreferences.Set(PreviousRootKey, string.Empty);
    }

    public bool IsDefault(string folder) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)), Path.TrimEndingDirectorySeparator(_location.DefaultRoot),
                      OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    // Everything in memory goes to disk first, and the combat recording ends.
    private async Task StopAndFlushAsync()
    {
        // The app's own writes from here on are not changes made elsewhere.
        _watcher.Stop();
        _combatLog.EndSession();
        await _combats.SaveAllAsync();
        await _automations.SaveAsync();
        await _store.FlushAsync();
    }

    // From here until the restart, nothing is written to the old folder: a
    // save would be lost, since the copy is already made.
    // Returns the files it blocked, so that a failed move lifts only those:
    // a file that failed to load stays blocked.
    private async Task<List<string>> BlockAllAsync()
    {
        var blocked = new List<string>();
        foreach (string name in BackupService.DataFileNames)
        {
            if (_store.IsBlocked(name)) continue;
            await _store.BlockAsync(name, "The data is being moved");
            blocked.Add(name);
        }
        return blocked;
    }

    // Each file is read again rather than forgotten: it then holds what was
    // just saved, and the next save is not taken for a change made elsewhere.
    private async Task UnblockAsync(IEnumerable<string> names)
    {
        foreach (string name in names)
        {
            var result = await _store.LoadTextAsync(name);
            if (result.Outcome == LoadOutcome.Failed) _store.ForgetFile(name);
        }
        if (IsLoaded && OperatingSystem.IsWindows()) _watcher.Start(_location.Root);
    }

    private void WriteMarkerTo(string target)
    {
        var current = DataFolderMarker.Read(_location.Root);
        DataFolderMarker.Create(_location.AppName, current?.LibraryId ?? Guid.NewGuid(), _location.MachineName, _combats.GetVersion())
                        .WriteTo(target);
    }
}

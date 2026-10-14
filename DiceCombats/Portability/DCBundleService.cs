/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Text.Json;
using DiceCombats.Automations;
using DiceCombats.Storage;
using DCImportItem = DiceCombats.Portability.ImportItem<DiceCombats.Portability.DCAssetKind>;
using DCImportPlan = DiceCombats.Portability.ImportPlan<DiceCombats.Portability.DCAssetKind>;

namespace DiceCombats.Portability;

/// <summary>A file being imported: what it holds, and what to do with each part of it.</summary>
public sealed class DCImportSession
{
    public DCImportSession(DCBundle bundle, DCImportPlan plan, ConflictPlanner planner)
    {
        Bundle = bundle;
        Plan = plan;
        Planner = planner;
    }

    public DCBundle Bundle { get; }
    public DCImportPlan Plan { get; }
    public ConflictPlanner Planner { get; }
}

/// <summary>A data file the import would write could not be read at startup; nothing has been changed.</summary>
public sealed class ImportBlockedException : Exception
{
    public ImportBlockedException(string fileName) : base($"{fileName} is blocked") { FileName = fileName; }
    public string FileName { get; }
}

/// <summary>A new name chosen in the wizard is empty or already used; nothing has been changed.</summary>
public sealed class ImportNameException : Exception
{
    public ImportNameException(string name) : base($"The name '{name}' cannot be used") { Name = name; }
    public string Name { get; }
}

/// <summary>The import failed; the data has been put back from the before-import backup.</summary>
public sealed class ImportFailedException : Exception
{
    public ImportFailedException(Exception inner) : base(inner.Message, inner) { }
}

/// <summary>The import failed and putting the data back failed too; the before-import backup is named here.</summary>
public sealed class ImportRollbackFailedException : Exception
{
    public ImportRollbackFailedException(string backupFileName, Exception inner) : base(inner.Message, inner)
    {
        BackupFileName = backupFileName;
    }

    public string BackupFileName { get; }
}

/// <summary>
/// Export and import of DiceCombats files (spec §3.2): builds a bundle and
/// hands it to the save picker; reads any DiceCombats file, analyses it
/// against the library and applies the user's choices.
/// </summary>
public sealed class DCBundleService
{
    private static readonly string[] ImportedFiles =
    {
        DiceCombatsService.CreaturesFileName,
        DiceCombatsService.CombatsFileName,
        DiceCombatsService.PlayerGroupsFileName,
        DiceCombatsService.UserCreatureCustomFieldsFileName,
        AutomationConfigurationService.AutomationsFileName,
    };

    private readonly DiceCombatsService _combats;
    private readonly AutomationConfigurationService _automations;
    private readonly IFileHandler _fileHandler;
    private readonly BackupService _backups;
    private readonly CombatLogService _combatLog;
    private readonly SafeFileStore _store;

    public DCBundleService(DiceCombatsService combats, AutomationConfigurationService automations, IFileHandler fileHandler,
                           BackupService backups, CombatLogService combatLog, SafeFileStore store)
    {
        _combats = combats;
        _automations = automations;
        _fileHandler = fileHandler;
        _backups = backups;
        _combatLog = combatLog;
        _store = store;
    }

    // ── export ──────────────────────────────────────────────────────────

    /// <summary>Returns false when the user cancels the save picker; throws when the file cannot be written.</summary>
    public async Task<bool> ExportCreaturesAsync(IEnumerable<DCCreature> creatures, string fileName)
    {
        var collector = await CreateCollectorAsync();
        return await SaveAsync(collector.ForCreatures(creatures), fileName);
    }

    public async Task<bool> ExportCombatsAsync(IEnumerable<DCCombat> combats, string fileName)
    {
        var collector = await CreateCollectorAsync();
        return await SaveAsync(collector.ForCombats(combats), fileName);
    }

    public async Task<bool> ExportLibraryAsync(string fileName)
    {
        var collector = await CreateCollectorAsync();
        return await SaveAsync(collector.ForLibrary(), fileName);
    }

    // ── import ──────────────────────────────────────────────────────────

    /// <summary>Reads and analyses a file (pipeline steps 1 and 2). Writes nothing.</summary>
    /// <exception cref="DCBundleReadException">The file is refused.</exception>
    public async Task<DCImportSession> AnalyseAsync(byte[] data)
    {
        var bundle = DCBundleSerializer.Read(data);
        var configuration = await _automations.GetConfigurationAsync();
        var local = new DCLocalLibrary(_combats.GetCreatureList().ToList(), _combats.GetCombatList().ToList(),
                                       _combats.GetPlayerGroups().ToList(), _combats.GetUserCreatureCustomFields().ToList(),
                                       configuration.ExternalApps.ToList(), configuration.Bindings.ToList());
        var planner = new ConflictPlanner(local);
        return new DCImportSession(bundle, planner.Analyse(bundle), planner);
    }

    /// <summary>
    /// Applies the choices (pipeline step 4): a before-import backup, the end of
    /// the combat recording, the GUID map, the copies inserted or replaced, and
    /// every save awaited and checked. On any failure the data is put back from
    /// the backup.
    /// </summary>
    /// <exception cref="ImportNameException">Nothing has been changed.</exception>
    /// <exception cref="ImportBlockedException">Nothing has been changed.</exception>
    /// <exception cref="BackupSafetyFailedException">The backup could not be made; nothing has been changed.</exception>
    /// <exception cref="ImportFailedException">The data has been put back.</exception>
    /// <exception cref="ImportRollbackFailedException">Putting the data back failed too.</exception>
    public async Task<ImportReport> ApplyAsync(DCImportSession session)
    {
        // The names are checked again here, so that a name typed in the
        // wizard but not checked yet can never reach the library.
        var invalid = session.Planner.InvalidNewNames(session.Plan);
        if (invalid.Count > 0) throw new ImportNameException(invalid[0].NewName ?? "");

        // A file that could not be read is never saved over: what is in memory
        // for it is not what the file holds.
        foreach (string file in ImportedFiles)
        {
            if (_store.IsBlocked(file)) throw new ImportBlockedException(file);
        }

        BackupInfo backup;
        try
        {
            backup = await _backups.CreateBackupAsync(BackupReason.BeforeImport);
        }
        catch (Exception ex)
        {
            throw new BackupSafetyFailedException(ex);
        }

        _combatLog.EndSession();
        try
        {
            return await ApplyCoreAsync(session);
        }
        catch (Exception ex)
        {
            try
            {
                await _backups.RestoreAsync(await File.ReadAllBytesAsync(backup.Path));
            }
            catch (Exception rollbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"Putting the data back after an import failed: {rollbackEx}");
                throw new ImportRollbackFailedException(backup.FileName, ex);
            }
            throw new ImportFailedException(ex);
        }
    }

    private async Task<ImportReport> ApplyCoreAsync(DCImportSession session)
    {
        var plan = session.Plan;
        var bundle = session.Bundle;
        IdRemapper.BuildFrom(plan).Rewrite(bundle);

        // A creature imported under another name: its entries in the
        // imported combats take that name too.
        var renamedCreatures = plan.Items
            .Where(i => i.Kind == DCAssetKind.Creature && i.NeedsNewName && i.NewName != null)
            .ToDictionary(i => ((DCCreature)i.Incoming).Id, i => i.NewName!.Trim());
        foreach (var entry in bundle.Combats.SelectMany(c => c.CreaturesList))
        {
            if (renamedCreatures.TryGetValue(entry.Id, out string? name)) entry.Rename(name);
        }

        var configuration = await _automations.GetConfigurationAsync();
        int added = 0, replaced = 0, kept = 0, skipped = 0, upToDate = 0;

        foreach (var item in plan.Items.OrderBy(i => i.Kind))
        {
            switch (item.Choice)
            {
                case ImportChoice.Nothing: upToDate++; continue;
                case ImportChoice.Skip: skipped++; continue;
                case ImportChoice.KeepLocal:
                case ImportChoice.UseLocal: kept++; continue;
                case ImportChoice.ReplaceLocal: Replace(item, configuration); replaced++; continue;
                default:
                    // A user custom field the library refuses (its title is
                    // taken) is counted as not imported.
                    if (Insert(item, configuration)) added++;
                    else skipped++;
                    continue;
            }
        }

        // Every save is awaited, and one that wrote nothing it should have
        // fails the import, which is then rolled back.
        var outcomes = new List<(string File, SaveOutcome Outcome)>
        {
            (DiceCombatsService.CreaturesFileName, await _combats.SaveCreaturesAsync()),
            (DiceCombatsService.CombatsFileName, await _combats.SaveCombatsAsync()),
            (DiceCombatsService.PlayerGroupsFileName, await _combats.SavePlayerGroupsListAsync()),
            (DiceCombatsService.UserCreatureCustomFieldsFileName, await _combats.SaveUserCreatureCustomFieldsListAsync()),
        };
        await _automations.SaveAsync();
        await _store.FlushAsync();
        if (_store.IsBlocked(AutomationConfigurationService.AutomationsFileName))
        {
            outcomes.Add((AutomationConfigurationService.AutomationsFileName, SaveOutcome.Blocked));
        }
        var failed = outcomes.Where(o => o.Outcome is SaveOutcome.Blocked or SaveOutcome.ChangedOnDisk).ToList();
        if (failed.Count > 0)
        {
            throw new IOException("Not saved: " + string.Join(", ", failed.Select(f => $"{f.File} ({f.Outcome})")));
        }

        return new ImportReport(added, replaced, kept, 0, skipped, upToDate, Array.Empty<string>());
    }

    private bool Insert(DCImportItem item, AutomationConfiguration configuration)
    {
        string? newName = item.NeedsNewName ? item.NewName?.Trim() : null;
        switch (item.Incoming)
        {
            case DCCreature creature:
                var creatureCopy = (DCCreature)creature.Clone();
                if (newName != null) creatureCopy.Rename(newName);
                _combats.AddCreature(creatureCopy);
                break;
            case DCCombat combat:
                var combatCopy = Copy(combat);
                if (newName != null) combatCopy.Name = newName;
                _combats.AddCombat(combatCopy);
                break;
            case DCPlayerGroup group:
                var groupCopy = Copy(group);
                if (newName != null) groupCopy.Name = newName;
                _combats.GetPlayerGroups().Add(groupCopy);
                break;
            case DCCreatureCustomField field:
                var fieldCopy = field.Clone();
                if (newName != null) fieldCopy.Title = newName;
                return _combats.AddUserCreatureCustomField(fieldCopy);
            case ExternalAppDefinition app:
                var appCopy = Copy(app);
                if (newName != null) appCopy.Name = newName;
                configuration.ExternalApps.Add(appCopy);
                break;
            case EventActionBinding binding:
                var bindingCopy = Copy(binding);
                if (newName != null) bindingCopy.Name = newName;
                configuration.Bindings.Add(bindingCopy);
                break;
        }
        return true;
    }

    // The local asset is overwritten by the imported one, in place, keeping
    // its GUID; a secret the export left out never replaces a local one.
    private void Replace(DCImportItem item, AutomationConfiguration configuration)
    {
        switch (item.Incoming)
        {
            case DCCreature creature:
                ReplaceIn(_combats.GetCreatureList(), item.Local, (DCCreature)creature.Clone());
                break;
            case DCCombat combat:
                ReplaceIn(_combats.GetCombatList(), item.Local, Copy(combat));
                break;
            case DCPlayerGroup group:
                ReplaceIn(_combats.GetPlayerGroups(), item.Local, Copy(group));
                break;
            case DCCreatureCustomField field:
                ReplaceIn(_combats.GetUserCreatureCustomFields(), item.Local, field.Clone());
                break;
            case ExternalAppDefinition app:
                var appCopy = Copy(app);
                if (item.Local is ExternalAppDefinition localApp)
                {
                    if (string.IsNullOrEmpty(appCopy.BearerToken)) appCopy.BearerToken = localApp.BearerToken;
                    KeepLocalSecrets(appCopy.DefaultHeaders, localApp.DefaultHeaders);
                }
                ReplaceIn(configuration.ExternalApps, item.Local, appCopy);
                break;
            case EventActionBinding binding:
                var bindingCopy = Copy(binding);
                if (item.Local is EventActionBinding localBinding)
                {
                    foreach (var http in bindingCopy.Actions.OfType<HttpRequestActionDefinition>())
                    {
                        var localHttp = localBinding.Actions.OfType<HttpRequestActionDefinition>().FirstOrDefault(a => a.Id == http.Id);
                        if (localHttp != null) KeepLocalSecrets(http.Headers, localHttp.Headers);
                    }
                }
                ReplaceIn(configuration.Bindings, item.Local, bindingCopy);
                break;
        }
    }

    private static void ReplaceIn<T>(List<T> list, object? local, T value) where T : class
    {
        int index = local is T existing ? list.IndexOf(existing) : -1;
        if (index < 0) throw new InvalidOperationException("The local item to replace is no longer in the library.");
        list[index] = value;
    }

    private static void KeepLocalSecrets(Dictionary<string, string> incoming, Dictionary<string, string> local)
    {
        foreach (string name in incoming.Keys.ToList())
        {
            if (DCBundleCollector.IsSecretHeader(name) && string.IsNullOrEmpty(incoming[name])
                && local.TryGetValue(name, out string? value) && !string.IsNullOrEmpty(value))
            {
                incoming[name] = value;
            }
        }
    }

    private async Task<DCBundleCollector> CreateCollectorAsync()
    {
        var automations = await _automations.GetConfigurationAsync();
        return new DCBundleCollector(_combats.GetCreatureList(), _combats.GetCombatList(), _combats.GetPlayerGroups(),
                                     _combats.GetUserCreatureCustomFields(), automations, _combats.GetVersion());
    }

    private Task<bool> SaveAsync(DCBundle bundle, string fileName)
    {
        return _fileHandler.SaveFileAsync(fileName, DCBundleSerializer.Write(bundle), ".json", "application/json", "JSON File");
    }

    private static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
}

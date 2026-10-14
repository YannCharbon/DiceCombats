/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using DiceCombats.Automations;

namespace DiceCombats.Portability;

/// <summary>What the simple import added, and what it left because the same GUID was already there.</summary>
public sealed record DCSimpleImportResult(int Added, int KeptLocal);

/// <summary>
/// The pages' way to export and import DiceCombats files: builds the bundle,
/// hands it to the save picker, and reads any DiceCombats file back.
/// </summary>
public sealed class DCBundleService
{
    private readonly DiceCombatsService _combats;
    private readonly AutomationConfigurationService _automations;
    private readonly IFileHandler _fileHandler;

    public DCBundleService(DiceCombatsService combats, AutomationConfigurationService automations, IFileHandler fileHandler)
    {
        _combats = combats;
        _automations = automations;
        _fileHandler = fileHandler;
    }

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

    /// <summary>
    /// Until the import with choices exists: adds the creatures and combats of
    /// any DiceCombats file (a bundle or an older export) whose GUID is not in
    /// the library yet, keeps the local ones otherwise, and saves.
    /// </summary>
    /// <exception cref="DCBundleReadException">The file is refused; nothing has been changed.</exception>
    public async Task<DCSimpleImportResult> ImportSimpleAsync(byte[] data)
    {
        var bundle = DCBundleSerializer.Read(data);
        int added = 0, kept = 0;

        foreach (var creature in bundle.Creatures)
        {
            if (_combats.GetCreatureFromGUID(creature.Id.ToString()) == null) { _combats.AddCreature(creature); added++; }
            else kept++;
        }
        foreach (var combat in bundle.Combats)
        {
            if (_combats.GetCombatFromGUID(combat.Id.ToString()) == null) { _combats.AddCombat(combat); added++; }
            else kept++;
        }

        if (added > 0)
        {
            await _combats.SaveCreaturesAsync();
            await _combats.SaveCombatsAsync();
        }
        return new DCSimpleImportResult(added, kept);
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
}

/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

namespace DiceCombats.Portability;

public enum DCImportStage { Idle, Refused, Summary, Wizard, Applying, Report, Failed }

/// <summary>
/// One import from start to end, shared by every place that offers "Import
/// from a file…" and by ImportWizard, which shows its stages. Kept outside the
/// pages so that the wizard survives a page change.
/// </summary>
public sealed class DCImportFlow
{
    private readonly DCBundleService _bundles;
    private readonly IFileHandler _fileHandler;
    private TaskCompletionSource? _done;
    private ImportReport? _applied;

    public DCImportFlow(DCBundleService bundles, IFileHandler fileHandler)
    {
        _bundles = bundles;
        _fileHandler = fileHandler;
    }

    /// <summary>Raised from any thread when the stage changes.</summary>
    public event Action? Changed;

    public DCImportStage Stage { get; private set; } = DCImportStage.Idle;
    public DCImportSession? Session { get; private set; }
    public ImportReport? Report { get; private set; }
    public Exception? Error { get; private set; }
    public long FileSizeBytes { get; private set; }

    /// <summary>
    /// Asks for a file and opens the import. Completes when the user closes it
    /// (or at once when the file dialog is cancelled), so the caller can show
    /// what changed.
    /// </summary>
    public async Task ImportFromFileAsync()
    {
        if (Stage != DCImportStage.Idle) return;
        var data = await _fileHandler.LoadFileAsync();
        if (data == null || data.Length == 0) return;
        await StartAsync(data);
    }

    /// <summary>Opens the import of a file. Returns the report when the user applied it, null otherwise.</summary>
    public async Task<ImportReport?> StartAsync(byte[] data)
    {
        if (Stage != DCImportStage.Idle) return null;
        _applied = null;
        _done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        FileSizeBytes = data.LongLength;
        try
        {
            Session = await _bundles.AnalyseAsync(data);
            Stage = Session.Plan.HasConflicts ? DCImportStage.Wizard : DCImportStage.Summary;
        }
        catch (DCBundleReadException ex)
        {
            Error = ex;
            Stage = DCImportStage.Refused;
        }
        Changed?.Invoke();
        await _done.Task;
        return _applied;
    }

    public async Task ConfirmAsync()
    {
        if (Session == null || Stage is not (DCImportStage.Summary or DCImportStage.Wizard)) return;

        // A name that is empty or taken sends the user back to the wizard,
        // where the problem is shown next to the name.
        if (Session.Planner.InvalidNewNames(Session.Plan).Count > 0)
        {
            Stage = DCImportStage.Wizard;
            Changed?.Invoke();
            return;
        }

        Stage = DCImportStage.Applying;
        Changed?.Invoke();
        try
        {
            Report = await _bundles.ApplyAsync(Session);
            _applied = Report;
            Stage = DCImportStage.Report;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Import failed: {ex}");
            Error = ex;
            Stage = DCImportStage.Failed;
        }
        Changed?.Invoke();
    }

    /// <summary>Closes the import, whatever its stage; nothing is applied if it was not confirmed.</summary>
    public void Close()
    {
        if (Stage == DCImportStage.Applying) return;
        Stage = DCImportStage.Idle;
        Session = null;
        Report = null;
        Error = null;
        Changed?.Invoke();
        _done?.TrySetResult();
    }
}

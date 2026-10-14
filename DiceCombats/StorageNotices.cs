/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using DiceCombats.Storage;

namespace DiceCombats
{
    public enum StorageNoticeKind { LoadFailed, SaveFailed, RestoreDone, RestoreFailed, RestoreRollbackFailed, ChangedOnDisk, ConflictCopies, CloudOnly, UnsavedCopies, UnsavedCopiesManual }

    /// <param name="Detail">The file name for a load or save failure, the reason for a failed restore,
    /// the safety backup's file name when putting the data back failed.</param>
    public sealed record StorageNotice(StorageNoticeKind Kind, string Detail);

    /// <summary>
    /// What the user must be told about reading and writing the data files:
    /// a file that could not be read (once per session), a save that failed,
    /// and how a restore ended. Kept outside the pages, so that a notice raised while no page
    /// listens, or before the UI is rebuilt for a language change, still
    /// reaches the screen.
    /// </summary>
    public sealed class StorageNotices
    {
        private readonly SafeFileStore _store;
        private readonly object _gate = new();
        private readonly List<StorageNotice> _pending = new();
        private readonly HashSet<string> _loadFailuresReported = new(StringComparer.OrdinalIgnoreCase);

        public StorageNotices(SafeFileStore store)
        {
            _store = store;
            _store.SaveFailed += (fileName, _) => Add(new StorageNotice(StorageNoticeKind.SaveFailed, fileName));
        }

        /// <summary>Raised from any thread when the pending list changes.</summary>
        public event Action? Changed;

        public IReadOnlyList<StorageNotice> Pending
        {
            get { lock (_gate) { return _pending.ToList(); } }
        }

        /// <summary>Adds a notice for each file that failed to load and has not been reported yet.</summary>
        public void ReportLoadFailures()
        {
            foreach (string fileName in _store.BlockedFiles)
            {
                bool isNew;
                lock (_gate)
                {
                    isNew = _loadFailuresReported.Add(fileName);
                }
                if (isNew)
                {
                    Add(new StorageNotice(StorageNoticeKind.LoadFailed, fileName));
                }
            }
        }

        /// <summary>Told after the pages are rebuilt, so it is kept here rather than in a page.</summary>
        public void ReportRestore(bool succeeded, string detail = "")
        {
            Add(new StorageNotice(succeeded ? StorageNoticeKind.RestoreDone : StorageNoticeKind.RestoreFailed, detail));
        }

        /// <summary>A save found its file changed on another computer: the user chooses to reload or overwrite.</summary>
        public void ReportChangedOnDisk(string fileName)
        {
            Add(new StorageNotice(StorageNoticeKind.ChangedOnDisk, fileName));
        }

        /// <summary>At startup: conflict copies left by a synchronisation service, or files only in the cloud (names, comma-separated).</summary>
        public void ReportCloudFiles(IReadOnlyList<string> conflictCopies, IReadOnlyList<string> cloudOnly)
        {
            if (conflictCopies.Count > 0) Add(new StorageNotice(StorageNoticeKind.ConflictCopies, string.Join(", ", conflictCopies)));
            if (cloudOnly.Count > 0) Add(new StorageNotice(StorageNoticeKind.CloudOnly, string.Join(", ", cloudOnly)));
        }

        /// <summary>
        /// At startup: changes kept at the last close because the file had been
        /// changed on another computer. Those that Import from a file... reads
        /// (creature and combat lists), and those to put back by hand.
        /// </summary>
        public void ReportUnsavedCopies(IReadOnlyList<string> importable, IReadOnlyList<string> manual)
        {
            if (importable.Count > 0) Add(new StorageNotice(StorageNoticeKind.UnsavedCopies, string.Join(", ", importable)));
            if (manual.Count > 0) Add(new StorageNotice(StorageNoticeKind.UnsavedCopiesManual, string.Join(", ", manual)));
        }

        /// <summary>Removes one notice (a choice was made for it).</summary>
        public void Dismiss(StorageNotice notice)
        {
            lock (_gate)
            {
                _pending.Remove(notice);
            }
            Changed?.Invoke();
        }

        /// <summary>A restore failed and the data could not be put back: the user restores this backup by hand.</summary>
        public void ReportRollbackFailed(string safetyBackupFileName)
        {
            Add(new StorageNotice(StorageNoticeKind.RestoreRollbackFailed, safetyBackupFileName));
        }

        public void DismissAll()
        {
            lock (_gate)
            {
                _pending.Clear();
            }
            Changed?.Invoke();
        }

        private void Add(StorageNotice notice)
        {
            lock (_gate)
            {
                // The same failure repeated while the first is still on screen
                // adds nothing.
                if (_pending.Contains(notice)) return;
                _pending.Add(notice);
            }
            Changed?.Invoke();
        }
    }
}

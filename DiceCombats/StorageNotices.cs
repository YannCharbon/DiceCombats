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
    public enum StorageNoticeKind { LoadFailed, SaveFailed }

    public sealed record StorageNotice(StorageNoticeKind Kind, string FileName);

    /// <summary>
    /// What the user must be told about reading and writing the data files:
    /// a file that could not be read (once per session) and a save that
    /// failed. Kept outside the pages, so that a notice raised while no page
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

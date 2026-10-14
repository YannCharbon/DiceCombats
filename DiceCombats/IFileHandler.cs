using System;
/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Threading.Tasks;

namespace DiceCombats
{
    public interface IFileHandler
    {
        Task SaveFileAsync(string fileName, byte[] data);

        /// Saves through the save picker with the given file type. Returns false
        /// when the user cancels; throws when the file cannot be written.
        Task<bool> SaveFileAsync(string fileName, byte[] data, string extension, string mimeType, string typeLabel);

        /// The full path of a folder the user picks, or null when cancelled or
        /// not available (Android: the data folder cannot be moved there).
        Task<string?> PickFolderAsync();
        Task<byte[]> LoadFileAsync();

        // New: used by DetailedPopup to let the user choose any file and read its bytes.
        // Returns (a best-effort display path/name, and the file bytes). Empty values if cancelled.
        Task<(string path, byte[] data)> PickFileAsync();
    }
}

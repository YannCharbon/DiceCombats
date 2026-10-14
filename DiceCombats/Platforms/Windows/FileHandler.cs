/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DiceCombats
{
    public class FileHandler : IFileHandler
    {
        public async Task SaveFileAsync(string fileName, byte[] data)
        {
            await SaveFileAsync(fileName, data, ".json", "application/json", "JSON File");
        }

        public async Task<bool> SaveFileAsync(string fileName, byte[] data, string extension, string mimeType, string typeLabel)
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            picker.FileTypeChoices.Add(typeLabel, new List<string> { extension });
            picker.SuggestedFileName = Path.GetFileNameWithoutExtension(fileName);

            var winuiWindow = App.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as MauiWinUIWindow;
            if (winuiWindow == null)
                return false;

            var hwnd = winuiWindow.WindowHandle;
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file == null)
                return false;

            using var stream = await file.OpenStreamForWriteAsync();
            stream.SetLength(0);
            await stream.WriteAsync(data, 0, data.Length);
            return true;
        }

        public async Task<byte[]> LoadFileAsync()
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            picker.FileTypeFilter.Add(".json");

            var winuiWindow = App.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as MauiWinUIWindow;
            if (winuiWindow == null)
                return Array.Empty<byte>();

            var hwnd = winuiWindow.WindowHandle;
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                using var stream = await file.OpenStreamForReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }

            return Array.Empty<byte>();
        }

        public async Task<(string path, byte[] data)> PickFileAsync()
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            picker.FileTypeFilter.Add("*");

            var winuiWindow = App.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as MauiWinUIWindow;
            if (winuiWindow == null)
                return (string.Empty, Array.Empty<byte>());

            var hwnd = winuiWindow.WindowHandle;
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                using var stream = await file.OpenStreamForReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return (file.Path ?? file.Name, memoryStream.ToArray());
            }

            return (string.Empty, Array.Empty<byte>());
        }
    }
}

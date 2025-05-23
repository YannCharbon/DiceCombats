﻿/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using DiceCombats;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace DiceCombats
{
    public class FileHandler : IFileHandler
    {
        public async Task SaveFileAsync(string fileName, byte[] data)
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            picker.FileTypeChoices.Add("JSON File", new List<string> { ".json" });
            picker.SuggestedFileName = fileName;

            var winuiWindow = App.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as MauiWinUIWindow;
            if (winuiWindow == null)
                return;

            var hwnd = winuiWindow.WindowHandle;
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                using var stream = await file.OpenStreamForWriteAsync();
                stream.SetLength(0); // Clears the file content before writing new data
                await stream.WriteAsync(data, 0, data.Length);
            }
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
                return new byte[0];

            var hwnd = winuiWindow.WindowHandle;
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
            }

            return new byte[0];
        }
    }
}

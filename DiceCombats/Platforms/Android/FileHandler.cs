/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Database;
using Android.Provider;

namespace DiceCombats
{
    public class FileHandler : IFileHandler
    {
        public async Task SaveFileAsync(string fileName, byte[] data)
        {
            var activity = MainActivity.Instance;
            if (activity == null)
                throw new InvalidOperationException("Current activity is not available.");

            var intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("application/json");
            intent.PutExtra(Intent.ExtraTitle, fileName);

            var tcs = new TaskCompletionSource<bool>();

            void OnActivityResult(int requestCode, Result resultCode, Intent? resultData)
            {
                if (requestCode == 2000)
                {
                    MainActivity.ActivityResult -= OnActivityResult;

                    if (resultCode == Result.Ok && resultData?.Data != null)
                    {
                        try
                        {
                            var uri = resultData.Data;
                            using var outputStream = activity.ContentResolver?.OpenOutputStream(uri);
                            outputStream?.Write(data, 0, data.Length);
                            outputStream?.Flush();
                            tcs.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error saving file: {ex}");
                            tcs.SetResult(false);
                        }
                    }
                    else
                    {
                        tcs.SetResult(false);
                    }
                }
            }

            MainActivity.ActivityResult += OnActivityResult;
            activity.StartActivityForResult(intent, 2000);

            await tcs.Task;
        }

        public async Task<byte[]> LoadFileAsync()
        {
            var intent = new Intent(Intent.ActionGetContent);
            intent.SetType("*/*");
            intent.AddCategory(Intent.CategoryOpenable);

            var activity = MainActivity.Instance;
            if (activity == null)
                throw new InvalidOperationException("Current activity is not available.");

            var tcs = new TaskCompletionSource<byte[]>();

            void OnActivityResult(int requestCode, Result resultCode, Intent? data)
            {
                if (requestCode == 1000)
                {
                    MainActivity.ActivityResult -= OnActivityResult;

                    if (resultCode == Result.Ok && data?.Data != null)
                    {
                        var uri = data.Data;
                        using var stream = activity.ContentResolver?.OpenInputStream(uri);
                        using var memoryStream = new MemoryStream();
                        stream?.CopyTo(memoryStream);
                        tcs.SetResult(memoryStream.ToArray());
                    }
                    else
                    {
                        tcs.SetResult(Array.Empty<byte>());
                    }
                }
            }

            MainActivity.ActivityResult += OnActivityResult;
            activity.StartActivityForResult(intent, 1000);

            return await tcs.Task;
        }

        public async Task<(string path, byte[] data)> PickFileAsync()
        {
            var intent = new Intent(Intent.ActionGetContent);
            intent.SetType("*/*");
            intent.AddCategory(Intent.CategoryOpenable);

            var activity = MainActivity.Instance;
            if (activity == null)
                throw new InvalidOperationException("Current activity is not available.");

            var tcs = new TaskCompletionSource<(string path, byte[] data)>();

            void OnActivityResult(int requestCode, Result resultCode, Intent? data)
            {
                if (requestCode == 1500)
                {
                    MainActivity.ActivityResult -= OnActivityResult;

                    if (resultCode == Result.Ok && data?.Data != null)
                    {
                        var uri = data.Data;

                        string displayName = TryGetDisplayName(activity, uri) ?? (uri.LastPathSegment ?? string.Empty);

                        using var stream = activity.ContentResolver?.OpenInputStream(uri);
                        using var memoryStream = new MemoryStream();
                        stream?.CopyTo(memoryStream);

                        tcs.SetResult((displayName, memoryStream.ToArray()));
                    }
                    else
                    {
                        tcs.SetResult((string.Empty, Array.Empty<byte>()));
                    }
                }
            }

            MainActivity.ActivityResult += OnActivityResult;
            activity.StartActivityForResult(intent, 1500);

            return await tcs.Task;
        }

        private static string? TryGetDisplayName(Activity activity, Android.Net.Uri uri)
        {
            try
            {
                // Use the new Android.Provider.IOpenableColumns projection to avoid deprecation warnings.
                string[] projection = { Android.Provider.IOpenableColumns.DisplayName };
                using ICursor? cursor = activity.ContentResolver?.Query(uri, projection, null, null, null);
                if (cursor != null && cursor.MoveToFirst())
                {
                    int nameIndex = cursor.GetColumnIndex(Android.Provider.IOpenableColumns.DisplayName);
                    if (nameIndex >= 0)
                    {
                        return cursor.GetString(nameIndex);
                    }
                }
            }
            catch { /* ignore */ }
            return null;
        }
    }
}

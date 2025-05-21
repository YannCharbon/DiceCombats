/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using Android.App;
using Android.Content;
using DiceCombats;
using System.IO;
using System.Threading.Tasks;

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
            intent.PutExtra(Intent.ExtraTitle, fileName); // Suggest default file name

            var taskCompletionSource = new TaskCompletionSource<bool>();

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
                            using var outputStream = activity?.ContentResolver?.OpenOutputStream(uri);
                            outputStream?.Write(data, 0, data.Length);
                            outputStream?.Flush();
                            taskCompletionSource.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error saving file: {ex}");
                            taskCompletionSource.SetResult(false);
                        }
                    }
                    else
                    {
                        taskCompletionSource.SetResult(false);
                    }
                }
            }

            MainActivity.ActivityResult += OnActivityResult;
            activity.StartActivityForResult(intent, 2000);

            await taskCompletionSource.Task;
        }


        public async Task<byte[]> LoadFileAsync()
        {
            var intent = new Intent(Intent.ActionGetContent);
            intent.SetType("*/*");
            intent.AddCategory(Intent.CategoryOpenable);

            var activity = MainActivity.Instance;
            if (activity == null)
                throw new InvalidOperationException("Current activity is not available.");

            var taskCompletionSource = new TaskCompletionSource<byte[]>();

            MainActivity.ActivityResult += OnActivityResult;

            activity.StartActivityForResult(intent, 1000);

            return await taskCompletionSource.Task;

            void OnActivityResult(int requestCode, Result resultCode, Intent? data)
            {
                if (requestCode == 1000)
                {
                    MainActivity.ActivityResult -= OnActivityResult;

                    if (resultCode == Result.Ok && data?.Data != null)
                    {
                        var uri = data.Data;
                        var stream = activity?.ContentResolver?.OpenInputStream(uri);

                        using (var memoryStream = new MemoryStream())
                        {
                            stream?.CopyTo(memoryStream);
                            taskCompletionSource.SetResult(memoryStream.ToArray());
                        }
                    }
                    else
                    {
                        taskCompletionSource.SetResult(new byte[0]);
                    }
                }
            }
        }
    }
}

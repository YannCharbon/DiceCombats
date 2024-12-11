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
            var context = Android.App.Application.Context;
            var externalPath = context.GetExternalFilesDir(null).AbsolutePath;
            var filePath = Path.Combine(externalPath, fileName);

            await File.WriteAllBytesAsync(filePath, data);

            var fileUri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, context.PackageName + ".fileprovider", new Java.IO.File(filePath));
            var intent = new Intent(Intent.ActionSend);
            intent.SetType("*/*");
            intent.PutExtra(Intent.ExtraStream, fileUri);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);

            var chooser = Intent.CreateChooser(intent, "Share File");
            chooser.SetFlags(ActivityFlags.NewTask);
            context.StartActivity(chooser);
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

            MainActivity.ActivityResult += (requestCode, resultCode, data) =>
            {
                if (requestCode == 1000 && resultCode == Result.Ok)
                {
                    var uri = data?.Data;
                    if (uri != null)
                    {
                        var stream = activity.ContentResolver.OpenInputStream(uri);
                        using (var memoryStream = new MemoryStream())
                        {
                            stream.CopyTo(memoryStream);
                            taskCompletionSource.TrySetResult(memoryStream.ToArray());
                        }
                    }
                    else
                    {
                        taskCompletionSource.TrySetResult(null);
                    }
                }
                else
                {
                    taskCompletionSource.TrySetResult(null);
                }
            };

            activity.StartActivityForResult(intent, 1000);

            return await taskCompletionSource.Task;
        }
    }
}

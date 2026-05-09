using DiceCombats.Automations;

namespace DiceCombats.Integrations;

public sealed class MauiAutomationStorage : IAutomationStorage
{
    public async Task<string?> ReadTextAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, fileName);
        if (!File.Exists(path)) return null;
        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public async Task WriteTextAsync(string fileName, string content, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, fileName);
        await File.WriteAllTextAsync(path, content, cancellationToken);
    }
}

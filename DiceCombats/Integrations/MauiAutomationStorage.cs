using System.Text.Json;
using DiceCombats.Automations;
using DiceCombats.Storage;

namespace DiceCombats.Integrations;

/// <summary>Keeps automations.json in the data folder, through the same safe store as the other data files.</summary>
public sealed class MauiAutomationStorage : IAutomationStorage
{
    private readonly SafeFileStore _store;

    public MauiAutomationStorage(SafeFileStore store)
    {
        _store = store;
    }

    public async Task<T?> ReadJsonAsync<T>(string fileName, JsonSerializerOptions options, CancellationToken cancellationToken = default) where T : class
    {
        var result = await _store.LoadJsonAsync<T>(fileName, options, cancellationToken);
        return result.Outcome switch
        {
            LoadOutcome.Loaded => result.Value,
            LoadOutcome.Missing => null,
            _ => throw new InvalidDataException($"{fileName} could not be read: {result.Error}"),
        };
    }

    public async Task WriteTextAsync(string fileName, string content, CancellationToken cancellationToken = default)
    {
        // A change made on another computer is overwritten until DiceCombats
        // can ask what to do about it.
        await _store.SaveTextAsync(fileName, content, overwriteExternalChange: true, ct: cancellationToken);
    }

    public void Block(string fileName, string reason) => _store.Block(fileName, reason);
}

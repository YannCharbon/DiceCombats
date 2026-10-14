using System.Text.Json;
using DiceCombats.Automations;
using DiceCombats.Storage;

namespace DiceCombats.Integrations;

/// <summary>Keeps automations.json in the data folder, through the same safe store as the other data files.</summary>
public sealed class MauiAutomationStorage : IAutomationStorage
{
    private readonly SafeFileStore _store;
    private readonly StorageNotices? _notices;

    public MauiAutomationStorage(SafeFileStore store, StorageNotices? notices = null)
    {
        _store = store;
        _notices = notices;
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

    // A file changed on another computer is not written over: the user is
    // asked whether to reload it or overwrite it.
    public async Task WriteTextAsync(string fileName, string content, CancellationToken cancellationToken = default, bool overwriteExternalChange = false)
    {
        var outcome = await _store.SaveTextAsync(fileName, content, overwriteExternalChange, cancellationToken);
        if (outcome == SaveOutcome.ChangedOnDisk)
        {
            _notices?.ReportChangedOnDisk(fileName);
        }
    }

    public void Block(string fileName, string reason) => _store.Block(fileName, reason);
}

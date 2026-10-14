using System.Diagnostics;
using System.Text.Json;

namespace DiceCombats.Automations;

public interface IAutomationStorage
{
    /// <summary>
    /// Reads and deserialises a file. Returns null when the file does not
    /// exist. Throws when it exists but cannot be read or deserialised; the
    /// storage then never writes over it during this session.
    /// </summary>
    Task<T?> ReadJsonAsync<T>(string fileName, JsonSerializerOptions options, CancellationToken cancellationToken = default) where T : class;
    Task WriteTextAsync(string fileName, string content, CancellationToken cancellationToken = default);

    /// <summary>Refuses every later write of the file during this session.</summary>
    void Block(string fileName, string reason);
}

public sealed class AutomationConfigurationService
{
    private const string FileName = "automations.json";
    private readonly IAutomationStorage _storage;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private AutomationConfiguration _configuration = new();
    private bool _loaded;

    public AutomationConfigurationService(IAutomationStorage storage)
    {
        _storage = storage;
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    /// <summary>Loads the configuration once, at startup.</summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default) => EnsureLoadedAsync(cancellationToken);

    public async Task<AutomationConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _configuration;
    }

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded) return;
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_loaded) return;
            // A file that cannot be read leaves an empty configuration in
            // memory, and the storage refuses to save it over the file.
            try { _configuration = await _storage.ReadJsonAsync<AutomationConfiguration>(FileName, _jsonOptions, cancellationToken) ?? new(); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Debug.WriteLine($"Invalid automations json: {ex}");
                _configuration = new();
                _storage.Block(FileName, ex.Message);
            }
            _loaded = true;
        }
        finally { _lock.Release(); }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        var text = JsonSerializer.Serialize(_configuration, _jsonOptions);
        await _storage.WriteTextAsync(FileName, text, cancellationToken);
    }
}

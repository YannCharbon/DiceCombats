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
    public const string AutomationsFileName = "automations.json";
    private const string FileName = AutomationsFileName;
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

    /// <summary>
    /// Reads the file again, after a restore. The configuration in memory is
    /// replaced only once the file has been read (a missing file gives an empty
    /// one); a file that cannot be read keeps it as it was, and stays blocked.
    /// </summary>
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            try
            {
                var loaded = await _storage.ReadJsonAsync<AutomationConfiguration>(FileName, _jsonOptions, cancellationToken);
                _configuration = loaded ?? new();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Debug.WriteLine($"Invalid automations json: {ex}");
                _storage.Block(FileName, ex.Message);
            }
            _loaded = true;
        }
        finally { _lock.Release(); }
    }

    /// <summary>
    /// Reads the configuration at startup, once the data folder can be used.
    /// Always reads, so that nothing asked before that moment sticks.
    /// </summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default) => ReloadAsync(cancellationToken);

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
        // Nothing to save before the configuration has been read.
        if (!_loaded) return;
        var text = JsonSerializer.Serialize(_configuration, _jsonOptions);
        await _storage.WriteTextAsync(FileName, text, cancellationToken);
    }
}

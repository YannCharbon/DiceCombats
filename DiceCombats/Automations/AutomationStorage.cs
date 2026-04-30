using System.Diagnostics;
using System.Text.Json;

namespace DiceCombats.Automations;

public interface IAutomationStorage
{
    Task<string?> ReadTextAsync(string fileName, CancellationToken cancellationToken = default);
    Task WriteTextAsync(string fileName, string content, CancellationToken cancellationToken = default);
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
            var text = await _storage.ReadTextAsync(FileName, cancellationToken);
            if (!string.IsNullOrWhiteSpace(text))
            {
                try { _configuration = JsonSerializer.Deserialize<AutomationConfiguration>(text, _jsonOptions) ?? new(); }
                catch (Exception ex) { Debug.WriteLine($"Invalid automations json: {ex}"); _configuration = new(); }
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

using DiceCombats.Storage;

namespace DiceCombats.Integrations;

/// <summary>The local preferences of this machine, kept by MAUI.</summary>
public sealed class MauiLocalPreferences : ILocalPreferences
{
    public T Get<T>(string key, T defaultValue) => Preferences.Default.Get(key, defaultValue);

    public void Set<T>(string key, T value) => Preferences.Default.Set(key, value);

    public bool ContainsKey(string key) => Preferences.Default.ContainsKey(key);
}

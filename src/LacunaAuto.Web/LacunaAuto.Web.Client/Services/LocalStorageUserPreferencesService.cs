using System.Text.Json;
using LacunaAuto.UI.Shared.Models;
using LacunaAuto.UI.Shared.Services;
using Microsoft.JSInterop;

namespace LacunaAuto.Web.Client.Services;

public sealed class LocalStorageUserPreferencesService : IUserPreferencesService
{
    private const string StorageKey = "lacuna_user_preferences";
    private readonly IJSRuntime _jsRuntime;
    private UserPreferences? _cachedPreferences;

    public event Action? OnChanged;

    public LocalStorageUserPreferencesService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async ValueTask<UserPreferences> GetAsync()
    {
        if (_cachedPreferences is not null)
        {
            return _cachedPreferences;
        }

        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            
            if (!string.IsNullOrEmpty(json))
            {
                _cachedPreferences = JsonSerializer.Deserialize<UserPreferences>(json);
            }
        }
        catch
        {
            // If localStorage read fails, fall through to detection logic
        }

        if (_cachedPreferences is null)
        {
            // First run: detect from browser
            _cachedPreferences = await DetectPreferencesAsync();
            await SaveAsync(_cachedPreferences);
        }

        return _cachedPreferences;
    }

    public async ValueTask SaveAsync(UserPreferences preferences)
    {
        _cachedPreferences = preferences;
        
        try
        {
            var json = JsonSerializer.Serialize(preferences);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
            OnChanged?.Invoke();
        }
        catch
        {
            // Silently ignore localStorage write failures (e.g., private browsing)
        }
    }

    private async ValueTask<UserPreferences> DetectPreferencesAsync()
    {
        var preferences = new UserPreferences();

        try
        {
            // Detect language and regional format from navigator.language
            var browserLocale = await _jsRuntime.InvokeAsync<string>("eval", "navigator.language || 'en-US'");
            
            // Parse language code (e.g., "uk-UA" → language "uk", region "uk-UA")
            var parts = browserLocale.Split('-');
            if (parts.Length > 0)
            {
                var langCode = parts[0].ToLowerInvariant();
                
                // Map to supported language
                preferences.Language = langCode switch
                {
                    "uk" => "uk",
                    "ru" => "ru",
                    _ => "en"
                };
            }

            // Regional format uses the full locale
            preferences.RegionalFormat = browserLocale;

            // Detect currency from region
            preferences.CurrencyCode = browserLocale.ToLowerInvariant() switch
            {
                "uk-ua" => "UAH",
                var loc when loc.StartsWith("pl-") => "PLN",
                var loc when loc.StartsWith("en-us") => "USD",
                _ => "USD"
            };
        }
        catch
        {
            // If detection fails, use defaults (already set)
        }

        return preferences;
    }
}

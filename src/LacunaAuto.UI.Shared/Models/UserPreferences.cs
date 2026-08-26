namespace LacunaAuto.UI.Shared.Models;

public sealed class UserPreferences
{
    /// <summary>
    /// Language code for UI strings (e.g., "en", "uk", "ru").
    /// Maps to DefaultThreadCurrentUICulture for .resx selection.
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Regional format for numbers, dates, times (e.g., "en-US", "uk-UA", "ru-RU").
    /// Maps to DefaultThreadCurrentCulture for formatting.
    /// </summary>
    public string RegionalFormat { get; set; } = "en-US";

    /// <summary>
    /// Preferred currency code (ISO 4217, e.g., "USD", "EUR", "UAH").
    /// NOT applied to CultureInfo or used for listing price display.
    /// </summary>
    public string CurrencyCode { get; set; } = "USD";
}

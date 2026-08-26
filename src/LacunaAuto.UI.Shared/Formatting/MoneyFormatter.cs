using System.Globalization;

namespace LacunaAuto.UI.Shared.Formatting;

public static class MoneyFormatter
{
    /// <summary>
    /// Formats money with regional separators and currency suffix.
    /// Does NOT use currency symbol from CultureInfo (uses explicit currency code).
    /// </summary>
    /// <param name="amount">The amount to format</param>
    /// <param name="currencyCode">ISO 4217 currency code (e.g., "USD", "EUR", "UAH")</param>
    /// <param name="regionalFormat">Culture name for number formatting (e.g., "en-US", "uk-UA")</param>
    /// <returns>Formatted string with regional separators and currency code (e.g., "23 500 USD")</returns>
    public static string Format(decimal amount, string currencyCode, string regionalFormat)
    {
        try
        {
            var culture = new CultureInfo(regionalFormat);
            var formatted = amount.ToString("N0", culture.NumberFormat);
            return $"{formatted} {currencyCode}";
        }
        catch
        {
            // Fallback to invariant culture if regional format is invalid
            var formatted = amount.ToString("N0", CultureInfo.InvariantCulture);
            return $"{formatted} {currencyCode}";
        }
    }
}

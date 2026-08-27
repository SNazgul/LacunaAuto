using LacunaAuto.UI.Shared.Formatting;

namespace LacunaAuto.UI.Shared.Tests;

public sealed class MoneyFormatterTests
{
    [Fact]
    public void Format_UsesRegionalGroupingAndAppendsCurrencyCode()
    {
        var result = MoneyFormatter.Format(23500m, "USD", "en-US");

        Assert.Equal("23,500 USD", result);
    }
}

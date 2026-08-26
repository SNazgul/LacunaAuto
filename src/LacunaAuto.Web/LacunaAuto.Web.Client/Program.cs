using System.Globalization;
using LacunaAuto.UI.Shared.Services;
using LacunaAuto.Web.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register localization
builder.Services.AddLocalization();

// Register user preferences service
builder.Services.AddSingleton<IUserPreferencesService, LocalStorageUserPreferencesService>();

var host = builder.Build();

// Initialize culture from user preferences
var preferencesService = host.Services.GetRequiredService<IUserPreferencesService>();
var preferences = await preferencesService.GetAsync();

try
{
    // Language controls UI strings (.resx selection)
    var uiCulture = new CultureInfo(preferences.Language);
    CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
    
    // Regional format controls number/date formatting
    var formatCulture = new CultureInfo(preferences.RegionalFormat);
    CultureInfo.DefaultThreadCurrentCulture = formatCulture;
}
catch
{
    // If culture initialization fails, use English defaults
    CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en");
    CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
}

await host.RunAsync();

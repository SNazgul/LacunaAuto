using LacunaAuto.UI.Shared.Models;

namespace LacunaAuto.UI.Shared.Services;

public interface IUserPreferencesService
{
    ValueTask<UserPreferences> GetAsync();
    ValueTask SaveAsync(UserPreferences preferences);
    event Action? OnChanged;
}

using LacunaAuto.UI.Shared.Models;

namespace LacunaAuto.Web.Client.Services;

public interface IPublicListingsService
{
    public Task<IReadOnlyList<VehicleListingCardModel>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task<VehicleListingCardModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}

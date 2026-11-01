using LacunaAuto.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LacunaAuto.Data.Initialization;

public static class DevelopmentDatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LacunaAutoDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        await DevelopmentListingsSeeder.SeedAsync(dbContext, cancellationToken);
    }
}

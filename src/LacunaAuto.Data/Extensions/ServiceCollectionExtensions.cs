using LacunaAuto.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LacunaAuto.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLacunaAutoData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LacunaAutoDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("LacunaAuto");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'LacunaAuto' is required.");
            }

            options.UseNpgsql(connectionString);
        });

        return services;
    }
}

using LacunaAuto.Core.Listings;
using Microsoft.EntityFrameworkCore;

namespace LacunaAuto.Data.Persistence;

public sealed class LacunaAutoDbContext : DbContext
{
    public LacunaAutoDbContext(DbContextOptions<LacunaAutoDbContext> options)
        : base(options)
    {
    }

    public DbSet<VehicleListing> Listings => Set<VehicleListing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LacunaAutoDbContext).Assembly);
    }
}

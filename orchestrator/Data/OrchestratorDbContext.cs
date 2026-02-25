using Microsoft.EntityFrameworkCore;
using Orchestrator.Models;

namespace Orchestrator.Data;

public class OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options) : DbContext(options)
{
    public DbSet<FlowEvent> FlowEvents => Set<FlowEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FlowEvent>()
            .HasIndex(x => x.DeliveryId)
            .IsUnique();

        modelBuilder.Entity<FlowEvent>()
            .HasIndex(x => new { x.FlowId, x.ReceivedAt });

        modelBuilder.Entity<FlowEvent>()
            .Property(x => x.PayloadJson)
            .HasColumnType("TEXT");
    }
}

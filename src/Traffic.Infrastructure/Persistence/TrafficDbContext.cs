using Microsoft.EntityFrameworkCore;
using Traffic.Infrastructure.Persistence.Entities;

namespace Traffic.Infrastructure.Persistence;

public sealed class TrafficDbContext(DbContextOptions<TrafficDbContext> options)
    : DbContext(options)
{
    public DbSet<ExperimentRunEntity> ExperimentRuns => Set<ExperimentRunEntity>();

    public DbSet<TrafficMeasurementEntity> TrafficMeasurements => Set<TrafficMeasurementEntity>();

    public DbSet<SignalDecisionCommandEntity> SignalDecisionCommands => Set<SignalDecisionCommandEntity>();

    public DbSet<SignalStateSnapshotEntity> SignalStateSnapshots => Set<SignalStateSnapshotEntity>();

    public DbSet<SignalStateItemEntity> SignalStateItems => Set<SignalStateItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureExperimentRuns(modelBuilder);
        ConfigureTrafficMeasurements(modelBuilder);
        ConfigureSignalDecisionCommands(modelBuilder);
        ConfigureSignalStateSnapshots(modelBuilder);
        ConfigureSignalStateItems(modelBuilder);
    }

    private static void ConfigureExperimentRuns(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ExperimentRunEntity>();
        entity.ToTable("experiment_runs");
        entity.HasKey(run => run.Id);
        entity.Property(run => run.Policy).HasMaxLength(64).IsRequired();
        entity.Property(run => run.Scenario).HasMaxLength(128).IsRequired();
        entity.HasIndex(run => run.StartedAtUtc);
    }

    private static void ConfigureTrafficMeasurements(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<TrafficMeasurementEntity>();
        entity.ToTable("traffic_measurements");
        entity.HasKey(measurement => measurement.Id);
        entity.Property(measurement => measurement.IntersectionId).HasMaxLength(64).IsRequired();
        entity.Property(measurement => measurement.SignalId).HasMaxLength(64).IsRequired();
        entity.HasIndex(measurement => measurement.RunId);
        entity.HasIndex(measurement => measurement.MessageId);
        entity.HasIndex(measurement => new
        {
            measurement.IntersectionId,
            measurement.SignalId,
            measurement.MeasuredAtUtc
        });
    }

    private static void ConfigureSignalDecisionCommands(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SignalDecisionCommandEntity>();
        entity.ToTable("signal_decision_commands");
        entity.HasKey(command => command.Id);
        entity.Property(command => command.IntersectionId).HasMaxLength(64).IsRequired();
        entity.Property(command => command.SelectedSignalId).HasMaxLength(64).IsRequired();
        entity.Property(command => command.Policy).HasMaxLength(64).IsRequired();
        entity.HasIndex(command => command.RunId);
        entity.HasIndex(command => command.MessageId);
        entity.HasIndex(command => new
        {
            command.IntersectionId,
            command.IssuedAtUtc
        });
    }

    private static void ConfigureSignalStateSnapshots(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SignalStateSnapshotEntity>();
        entity.ToTable("signal_state_snapshots");
        entity.HasKey(snapshot => snapshot.Id);
        entity.Property(snapshot => snapshot.IntersectionId).HasMaxLength(64).IsRequired();
        entity.HasIndex(snapshot => snapshot.RunId);
        entity.HasIndex(snapshot => snapshot.MessageId);
        entity.HasIndex(snapshot => new
        {
            snapshot.IntersectionId,
            snapshot.CapturedAtUtc
        });
    }

    private static void ConfigureSignalStateItems(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SignalStateItemEntity>();
        entity.ToTable("signal_state_items");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.SignalId).HasMaxLength(64).IsRequired();
        entity.Property(item => item.State).HasMaxLength(32).IsRequired();
        entity.HasIndex(item => item.SnapshotId);

        entity
            .HasOne(item => item.Snapshot)
            .WithMany(snapshot => snapshot.Signals)
            .HasForeignKey(item => item.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

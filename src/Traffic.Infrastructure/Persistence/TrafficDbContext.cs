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
        entity.Property(run => run.Id).HasColumnName("id");
        entity.Property(run => run.Policy).HasColumnName("policy").HasMaxLength(64).IsRequired();
        entity.Property(run => run.Scenario).HasColumnName("scenario").HasMaxLength(128).IsRequired();
        entity.Property(run => run.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(run => run.FinishedAtUtc).HasColumnName("finished_at_utc");
        entity.HasIndex(run => run.StartedAtUtc);
    }

    private static void ConfigureTrafficMeasurements(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<TrafficMeasurementEntity>();
        entity.ToTable("traffic_measurements");
        entity.HasKey(measurement => measurement.Id);
        entity.Property(measurement => measurement.Id).HasColumnName("id");
        entity.Property(measurement => measurement.RunId).HasColumnName("run_id");
        entity.Property(measurement => measurement.MessageId).HasColumnName("message_id");
        entity.Property(measurement => measurement.IntersectionId)
            .HasColumnName("intersection_id")
            .HasMaxLength(64)
            .IsRequired();
        entity.Property(measurement => measurement.SignalId)
            .HasColumnName("signal_id")
            .HasMaxLength(64)
            .IsRequired();
        entity.Property(measurement => measurement.QueueLength).HasColumnName("queue_length");
        entity.Property(measurement => measurement.Arrivals).HasColumnName("arrivals");
        entity.Property(measurement => measurement.Departures).HasColumnName("departures");
        entity.Property(measurement => measurement.MeasuredAtUtc).HasColumnName("measured_at_utc");
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
        entity.Property(command => command.Id).HasColumnName("id");
        entity.Property(command => command.RunId).HasColumnName("run_id");
        entity.Property(command => command.MessageId).HasColumnName("message_id");
        entity.Property(command => command.IntersectionId)
            .HasColumnName("intersection_id")
            .HasMaxLength(64)
            .IsRequired();
        entity.Property(command => command.SelectedSignalId)
            .HasColumnName("selected_signal_id")
            .HasMaxLength(64)
            .IsRequired();
        entity.Property(command => command.SelectedPhaseId)
            .HasColumnName("selected_phase_id")
            .HasMaxLength(64);
        entity.Property(command => command.SelectedSignalIdsJson)
            .HasColumnName("selected_signal_ids_json")
            .HasColumnType("text")
            .HasDefaultValue("[]")
            .IsRequired();
        entity.Property(command => command.Policy).HasColumnName("policy").HasMaxLength(64).IsRequired();
        entity.Property(command => command.GreenDurationSeconds).HasColumnName("green_duration_seconds");
        entity.Property(command => command.YellowDurationSeconds).HasColumnName("yellow_duration_seconds");
        entity.Property(command => command.IssuedAtUtc).HasColumnName("issued_at_utc");
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
        entity.Property(snapshot => snapshot.Id).HasColumnName("id");
        entity.Property(snapshot => snapshot.RunId).HasColumnName("run_id");
        entity.Property(snapshot => snapshot.MessageId).HasColumnName("message_id");
        entity.Property(snapshot => snapshot.IntersectionId)
            .HasColumnName("intersection_id")
            .HasMaxLength(64)
            .IsRequired();
        entity.Property(snapshot => snapshot.CapturedAtUtc).HasColumnName("captured_at_utc");
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
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.SnapshotId).HasColumnName("snapshot_id");
        entity.Property(item => item.SignalId).HasColumnName("signal_id").HasMaxLength(64).IsRequired();
        entity.Property(item => item.State).HasColumnName("state").HasMaxLength(32).IsRequired();
        entity.Property(item => item.ElapsedSeconds).HasColumnName("elapsed_seconds");
        entity.Property(item => item.QueueLength).HasColumnName("queue_length");
        entity.HasIndex(item => item.SnapshotId);

        entity
            .HasOne(item => item.Snapshot)
            .WithMany(snapshot => snapshot.Signals)
            .HasForeignKey(item => item.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

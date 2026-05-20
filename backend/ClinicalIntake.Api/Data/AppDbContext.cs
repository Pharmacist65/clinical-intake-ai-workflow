using ClinicalIntake.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicalIntake.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Intake> Intakes => Set<Intake>();
    public DbSet<AiSummary> AiSummaries => Set<AiSummary>();
    public DbSet<RiskFlag> RiskFlags => Set<RiskFlag>();
    public DbSet<ContextEvent> ContextEvents => Set<ContextEvent>();
    public DbSet<MedicationEntry> MedicationEntries => Set<MedicationEntry>();
    public DbSet<MedicationSignal> MedicationSignals => Set<MedicationSignal>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Intake>(entity =>
        {
            entity.Property(intake => intake.PatientAlias).HasMaxLength(120).IsRequired();
            entity.Property(intake => intake.IntakeText).HasMaxLength(8000).IsRequired();
            entity.Property(intake => intake.Source).HasMaxLength(80).IsRequired();
            entity.Property(intake => intake.CreatedBy).HasMaxLength(120).IsRequired();
            entity.Property(intake => intake.ReviewStatus).HasConversion<string>().HasMaxLength(40);
            entity.HasOne(intake => intake.AiSummary)
                .WithOne(summary => summary.Intake)
                .HasForeignKey<AiSummary>(summary => summary.IntakeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(intake => intake.RiskFlags)
                .WithOne(flag => flag.Intake)
                .HasForeignKey(flag => flag.IntakeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(intake => intake.ContextEvents)
                .WithOne(contextEvent => contextEvent.Intake)
                .HasForeignKey(contextEvent => contextEvent.IntakeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(intake => intake.MedicationEntries)
                .WithOne(medication => medication.Intake)
                .HasForeignKey(medication => medication.IntakeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(intake => intake.MedicationSignals)
                .WithOne(signal => signal.Intake)
                .HasForeignKey(signal => signal.IntakeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(intake => intake.AuditLogs)
                .WithOne(log => log.Intake)
                .HasForeignKey(log => log.IntakeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiSummary>(entity =>
        {
            entity.Property(summary => summary.PresentingConcerns).HasMaxLength(2000).IsRequired();
            entity.Property(summary => summary.RelevantHistory).HasMaxLength(2000).IsRequired();
            entity.Property(summary => summary.PossibleRisks).HasMaxLength(2000).IsRequired();
            entity.Property(summary => summary.RecommendedNextStep).HasMaxLength(2000).IsRequired();
            entity.Property(summary => summary.Disclaimer).HasMaxLength(500).IsRequired();
            entity.HasIndex(summary => summary.IntakeId).IsUnique();
        });

        modelBuilder.Entity<RiskFlag>(entity =>
        {
            entity.Property(flag => flag.Label).HasMaxLength(120).IsRequired();
            entity.Property(flag => flag.Severity).HasConversion<string>().HasMaxLength(40);
            entity.Property(flag => flag.Reason).HasMaxLength(1000).IsRequired();
        });

        modelBuilder.Entity<ContextEvent>(entity =>
        {
            entity.Property(contextEvent => contextEvent.SourceType).HasConversion<string>().HasMaxLength(40);
            entity.Property(contextEvent => contextEvent.SourceLabel).HasMaxLength(120).IsRequired();
            entity.Property(contextEvent => contextEvent.Content).HasMaxLength(6000).IsRequired();
            entity.Property(contextEvent => contextEvent.CreatedBy).HasMaxLength(120).IsRequired();
            entity.Property(contextEvent => contextEvent.MetadataJson).HasMaxLength(2000);
        });

        modelBuilder.Entity<MedicationEntry>(entity =>
        {
            entity.Property(medication => medication.MedicationName).HasMaxLength(160).IsRequired();
            entity.Property(medication => medication.NormalizedName).HasMaxLength(160).IsRequired();
            entity.Property(medication => medication.Category).HasConversion<string>().HasMaxLength(40);
            entity.Property(medication => medication.Dose).HasMaxLength(120);
            entity.Property(medication => medication.Route).HasMaxLength(80);
            entity.Property(medication => medication.Frequency).HasMaxLength(120);
            entity.Property(medication => medication.ReasonForUse).HasMaxLength(500);
            entity.Property(medication => medication.Source).HasConversion<string>().HasMaxLength(40);
            entity.Property(medication => medication.PrescribedBy).HasMaxLength(160);
            entity.Property(medication => medication.Notes).HasMaxLength(1000);
        });

        modelBuilder.Entity<MedicationSignal>(entity =>
        {
            entity.Property(signal => signal.Label).HasMaxLength(160).IsRequired();
            entity.Property(signal => signal.Severity).HasConversion<string>().HasMaxLength(40);
            entity.Property(signal => signal.Rationale).HasMaxLength(1000).IsRequired();
            entity.Property(signal => signal.ReviewerQuestion).HasMaxLength(1000).IsRequired();
            entity.HasOne(signal => signal.MedicationEntry)
                .WithMany(medication => medication.MedicationSignals)
                .HasForeignKey(signal => signal.MedicationEntryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(log => log.Action).HasMaxLength(120).IsRequired();
            entity.Property(log => log.Actor).HasMaxLength(120).IsRequired();
            entity.Property(log => log.Details).HasMaxLength(1000).IsRequired();
        });
    }
}

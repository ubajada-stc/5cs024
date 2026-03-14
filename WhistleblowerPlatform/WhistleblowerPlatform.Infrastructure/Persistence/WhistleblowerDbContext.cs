using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WhistleblowerPlatform.Domain.Entities;

namespace WhistleblowerPlatform.Infrastructure.Persistence;

public partial class WhistleblowerDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public WhistleblowerDbContext(DbContextOptions<WhistleblowerDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<DeadlineTracking> DeadlineTrackings { get; set; }

    public virtual DbSet<Investigator> Investigators { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<NotificationQueue> NotificationQueues { get; set; }

    public virtual DbSet<PlatformSetting> PlatformSettings { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<ReportAttachment> ReportAttachments { get; set; }

    public virtual DbSet<ReportCategory> ReportCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // IMPORTANT: Call base method first - this sets up all Identity tables
        base.OnModelCreating(modelBuilder);

        // Configure the relationship between ApplicationUser and Investigator
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(u => u.Investigator)
                  .WithOne()
                  .HasForeignKey<ApplicationUser>(u => u.InvestigatorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // All existing entity configurations below (unchanged)

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Admins_Email").IsUnique();

            entity.Property(e => e.AdminId).ValueGeneratedNever();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Mfaenabled).HasColumnName("MFAEnabled");
            entity.Property(e => e.Mfasecret)
                .HasMaxLength(256)
                .HasColumnName("MFASecret");
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId);

            entity.HasIndex(e => e.Timestamp, "IX_AuditLogs_Timestamp");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.ActorId).HasMaxLength(256);
            entity.Property(e => e.Detail).HasMaxLength(1000);
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(45)
                .HasColumnName("IPAddress");
            entity.Property(e => e.TargetEntity).HasMaxLength(50);
            entity.Property(e => e.TargetId).HasMaxLength(256);
            entity.Property(e => e.UserAgent).HasMaxLength(512);
        });

        modelBuilder.Entity<DeadlineTracking>(entity =>
        {
            entity.HasKey(e => e.TrackingId);

            entity.ToTable("DeadlineTracking");

            entity.HasIndex(e => e.DueAt, "IX_DeadlineTracking_DueAt");

            entity.Property(e => e.TrackingId).ValueGeneratedNever();

            entity.HasOne(d => d.Report).WithMany(p => p.DeadlineTrackings)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeadlineTracking_Reports");
        });

        modelBuilder.Entity<Investigator>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Investigators_Email").IsUnique();

            entity.Property(e => e.InvestigatorId).ValueGeneratedNever();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Mfaenabled).HasColumnName("MFAEnabled");
            entity.Property(e => e.Mfasecret)
                .HasMaxLength(256)
                .HasColumnName("MFASecret");
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasIndex(e => e.ReportId, "IX_Messages_ReportId");

            entity.Property(e => e.MessageId).ValueGeneratedNever();

            entity.Property(e => e.WbkeyEnvelope).HasColumnName("WBKeyEnvelope");

            entity.HasOne(d => d.Report).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Messages_Reports");
        });

        modelBuilder.Entity<NotificationQueue>(entity =>
        {
            entity.HasKey(e => e.NotificationId);

            entity.ToTable("NotificationQueue");

            entity.HasIndex(e => e.IsSent, "IX_NotificationQueue_IsSent").HasFilter("([IsSent]=(0))");

            entity.Property(e => e.NotificationId).ValueGeneratedNever();
            entity.Property(e => e.Subject).HasMaxLength(256);

            entity.HasOne(d => d.Recipient).WithMany(p => p.NotificationQueues)
                .HasForeignKey(d => d.RecipientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotificationQueue_Investigators");
        });

        modelBuilder.Entity<PlatformSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId);

            entity.HasIndex(e => e.SettingKey, "UQ_PlatformSettings_SettingKey").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SettingKey).HasMaxLength(100);

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PlatformSettings)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PlatformSettings_Admins");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasIndex(e => e.TokenHash, "IX_Reports_TokenHash").IsUnique();

            entity.HasIndex(e => e.CaseNumber, "UQ_Reports_CaseNumber").IsUnique();

            entity.Property(e => e.ReportId).ValueGeneratedNever();
            entity.Property(e => e.CaseNumber).HasMaxLength(20);
            entity.Property(e => e.EncryptedWbprivateKey).HasColumnName("EncryptedWBPrivateKey");
            entity.Property(e => e.TokenHash).HasMaxLength(64);
            entity.Property(e => e.WbkeyEnvelope).HasColumnName("WBKeyEnvelope");
            entity.Property(e => e.WbkeySalt)
                .HasMaxLength(32)
                .HasColumnName("WBKeySalt");
            entity.Property(e => e.WbpublicKey).HasColumnName("WBPublicKey");
            entity.Property(e => e.Status).HasConversion<byte>();

            entity.HasOne(d => d.Category).WithMany(p => p.Reports)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Reports_ReportCategories");
        });

        modelBuilder.Entity<ReportAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId);

            entity.HasIndex(e => e.ReportId, "IX_ReportAttachments_ReportId");

            entity.Property(e => e.AttachmentId).ValueGeneratedNever();
            entity.Property(e => e.EncryptedFileName).HasMaxLength(512);
            entity.Property(e => e.MimeType).HasMaxLength(100);
            entity.Property(e => e.StoragePath).HasMaxLength(500);
            entity.Property(e => e.WbkeyEnvelope).HasColumnName("WBKeyEnvelope");

            // Sanitization columns (ADR-001)
            entity.Property(e => e.SanitizationStoragePath).HasMaxLength(500);
            entity.Property(e => e.SanitizationError).HasMaxLength(500);
            entity.Property(e => e.SanitizationStatus).HasConversion<byte>();

            entity.HasOne(d => d.Report).WithMany(p => p.ReportAttachments)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReportAttachments_Reports");
        });

        modelBuilder.Entity<ReportCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
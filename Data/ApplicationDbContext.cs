using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Entities.HealthRecords;

namespace SeasonsCare.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public Guid? CurrentCareGroupId { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<CareGroup> CareGroups { get; set; }
        public DbSet<CareGroupMember> CareGroupMembers { get; set; }
        public DbSet<CareLog> CareLogs { get; set; }
        public DbSet<EventSeries> EventSeries { get; set; }
        public DbSet<EventOccurrence> EventOccurrences { get; set; }
        public DbSet<AiHealthInsight> AiHealthInsights { get; set; }
        public DbSet<ExpenseRecord> Expenses { get; set; }
        public DbSet<BloodPressureRecord> BloodPressures { get; set; }
        public DbSet<BloodSugarRecord> BloodSugars { get; set; }
        public DbSet<WeightRecord> Weights { get; set; }
        public DbSet<TemperatureRecord> Temperatures { get; set; }
        public DbSet<BloodOxygenRecord> BloodOxygens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;
                if (typeof(ISoftDeleteEntity).IsAssignableFrom(clrType) || typeof(IMultiTenantEntity).IsAssignableFrom(clrType))
                {
                    var applyFiltersMethod = typeof(ApplicationDbContext).GetMethod(nameof(ApplyGlobalFilters), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                    applyFiltersMethod.MakeGenericMethod(clrType).Invoke(this, new object[] { modelBuilder });
                }
            }

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AvatarKey).IsRequired().HasMaxLength(50);
                entity.HasOne<CareGroup>()
                    .WithMany()
                    .HasForeignKey(e => e.LastViewedCareGroupId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CareGroup>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RecipientName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RecipientGender).HasMaxLength(20);
                entity.Property(e => e.RecipientBirthDate);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.HealthStatus).HasMaxLength(1000);
            });

            modelBuilder.Entity<CareGroupMember>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Foreign keys
                entity.HasOne(e => e.CareGroup)
                      .WithMany(g => g.Members)
                      .HasForeignKey(e => e.CareGroupId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Uniqueness: a user can only be in a specific group once
                entity.HasIndex(e => new { e.CareGroupId, e.UserId }).IsUnique();
            });

            modelBuilder.Entity<CareLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasColumnType("text").HasColumnName("content");
                entity.Property(e => e.StartsAt).HasColumnName("record_date");
                entity.Property(e => e.Status).HasMaxLength(50).HasColumnName("log_type");
                entity.Property(e => e.RepeatPattern).HasMaxLength(50);
                entity.Property(e => e.Participants).HasColumnType("text[]");
                entity.Property(e => e.IsImportant).HasDefaultValue(false);
                
                entity.HasOne(e => e.CareGroup)
                      .WithMany()
                      .HasForeignKey(e => e.CareGroupId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EventSeries>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.RepeatPattern).HasConversion<int>();
                entity.Property(e => e.EndType).HasConversion<int>();
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.Participants).HasColumnType("text[]");

                entity.HasOne(e => e.CareGroup)
                      .WithMany()
                      .HasForeignKey(e => e.CareGroupId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EventOccurrence>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OverrideTitle).HasMaxLength(100);
                entity.Property(e => e.OverrideDescription).HasColumnType("text");
                entity.Property(e => e.OverrideParticipants).HasColumnType("text[]");
                entity.Property(e => e.Status).HasConversion<int>();
                entity.HasIndex(e => new { e.EventSeriesId, e.ScheduledStartAt }).IsUnique();

                entity.HasOne(e => e.EventSeries)
                      .WithMany(x => x.Occurrences)
                      .HasForeignKey(e => e.EventSeriesId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CareGroup)
                      .WithMany()
                      .HasForeignKey(e => e.CareGroupId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AiHealthInsight>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ReportType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OverallSummary).HasColumnType("text");
                entity.Property(e => e.KeyInsights).HasColumnType("text");
                entity.Property(e => e.Recommendations).HasColumnType("text");
                entity.Property(e => e.SourceDataHash).HasMaxLength(128);
                entity.Property(e => e.ModelName).HasMaxLength(100);
                entity.Property(e => e.PromptVersion).HasMaxLength(50);
                entity.HasIndex(e => new { e.CareGroupId, e.ReportType, e.DateFrom, e.DateTo }).IsUnique();

                entity.HasOne(e => e.CareGroup)
                      .WithMany()
                      .HasForeignKey(e => e.CareGroupId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ExpenseRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.SplitStatus).HasConversion<string>().HasMaxLength(20);
                
                entity.HasOne(e => e.CareGroup)
                      .WithMany()
                      .HasForeignKey(e => e.CareGroupId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BloodPressureRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Notes).HasMaxLength(500);
                
                entity.HasOne(e => e.CareGroup)
                      .WithMany()
                      .HasForeignKey(e => e.CareGroupId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BloodSugarRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.GlucoseLevel).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MeasurementContext).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(500);
                
                entity.HasOne(e => e.CareGroup)
                      .WithMany()
                      .HasForeignKey(e => e.CareGroupId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WeightRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Value).HasColumnType("decimal(6,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);
                
                entity.HasOne(e => e.CareGroup)
                      .WithMany()
                      .HasForeignKey(e => e.CareGroupId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TemperatureRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Value).HasColumnType("decimal(6,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);
                
                entity.HasOne(e => e.CareGroup)
                      .WithMany()
                      .HasForeignKey(e => e.CareGroupId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BloodOxygenRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SpO2).HasColumnType("decimal(6,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);
                
                entity.HasOne(e => e.CareGroup)
                      .WithMany()
                      .HasForeignKey(e => e.CareGroupId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        // [架構導覽] 核心基礎設施 (Core Infrastructure) - 全域查詢過濾器 (Global Query Filter)
        // 職責：作為最底層的防護網。在查詢任何受保護的領域實體前，Entity Framework 將自動附加此設定，實現系統級的多租戶隔離 (Multi-Tenant) 與軟刪除 (Soft Delete) 過濾。
        private void ApplyGlobalFilters<T>(ModelBuilder modelBuilder) where T : class
        {
            bool isSoftDelete = typeof(ISoftDeleteEntity).IsAssignableFrom(typeof(T));
            bool isMultiTenant = typeof(IMultiTenantEntity).IsAssignableFrom(typeof(T));

            if (isSoftDelete && isMultiTenant)
            {
                modelBuilder.Entity<T>().HasQueryFilter(e => 
                    ((ISoftDeleteEntity)e).DeletedAt == null && 
                    (!CurrentCareGroupId.HasValue || ((IMultiTenantEntity)e).CareGroupId == CurrentCareGroupId.Value));
            }
            else if (isSoftDelete)
            {
                modelBuilder.Entity<T>().HasQueryFilter(e => ((ISoftDeleteEntity)e).DeletedAt == null);
            }
            else if (isMultiTenant)
            {
                modelBuilder.Entity<T>().HasQueryFilter(e => !CurrentCareGroupId.HasValue || ((IMultiTenantEntity)e).CareGroupId == CurrentCareGroupId.Value);
            }
        }
    }
}

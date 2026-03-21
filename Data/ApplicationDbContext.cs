using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Models.Entities;

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
        public DbSet<ExpenseRecord> Expenses { get; set; }

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
            });

            modelBuilder.Entity<CareGroup>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
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
                entity.Property(e => e.Content).HasColumnType("text");
                
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
                
                entity.HasOne(e => e.CareGroup)
                      .WithMany()
                      .HasForeignKey(e => e.CareGroupId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

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

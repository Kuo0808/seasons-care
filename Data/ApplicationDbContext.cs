using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<CareGroup> CareGroups { get; set; }
        public DbSet<CareGroupMember> CareGroupMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}

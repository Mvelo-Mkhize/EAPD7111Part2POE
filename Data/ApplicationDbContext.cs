using Microsoft.EntityFrameworkCore;
using EAPD7111Part2POE.Models.Entities;

namespace EAPD7111Part2POE.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Client>()
                .HasMany(c => c.Contracts)
                .WithOne(c => c.Client)
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Restrict);          

            modelBuilder.Entity<Contract>()
                .HasMany(c => c.ServiceRequests)
                .WithOne(s => s.Contract)
                .HasForeignKey(s => s.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Client>()
                .Property(c => c.CompanyName)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Client>()
                .Property(c => c.Email)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Client>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Contract>()
                .Property(c => c.ContractReference)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Contract>()
                .Property(c => c.ContractValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Contract>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Contract>()
                .HasIndex(c => c.ContractReference)
                .IsUnique();

            modelBuilder.Entity<ServiceRequest>()
                .Property(s => s.Cost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ServiceRequest>()
                .Property(s => s.CostInZAR)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ServiceRequest>()
                .Property(s => s.RequestDate)
                .HasDefaultValueSql("GETUTCDATE()");

        }
    }
}
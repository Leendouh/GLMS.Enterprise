using Microsoft.EntityFrameworkCore;
using GLMS.Enterprise.Core.Entities;
using GLMS.Enterprise.Core.Enums;

namespace GLMS.Enterprise.Infrastructure.Data;

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

        // ── Client ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // we set it ourselves
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.ContactEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ContactPhone).HasMaxLength(50);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);
        });

        // ── Contract ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ClientId).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
            entity.Property(e => e.Status)
                  .IsRequired()
                  .HasConversion(
                      v => v.ToString(),
                      v => (ContractStatus)Enum.Parse(typeof(ContractStatus), v));
            entity.Property(e => e.ServiceLevel).HasMaxLength(200);
            entity.Property(e => e.PdfFilePath).HasMaxLength(500);
            entity.Property(e => e.OriginalPdfFileName).HasMaxLength(255);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);

            // Indexes
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });

            // FK: Contract → Client (restrict delete)
            entity.HasOne(e => e.Client)
                  .WithMany(c => c.Contracts)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Check constraint: EndDate > StartDate
            entity.ToTable(t =>
                t.HasCheckConstraint("CK_Contract_EndDate_After_StartDate", "[EndDate] > [StartDate]"));
        });

        // ── ServiceRequest ────────────────────────────────────────────────────
        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ContractId).IsRequired();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AmountUSD).IsRequired().HasColumnType("decimal(18,2)");
            entity.ToTable(t =>
                t.HasCheckConstraint("CK_ServiceRequest_AmountUSD_Positive", "[AmountUSD] > 0"));
            entity.Property(e => e.AmountZAR).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.ExchangeRateUsed).IsRequired().HasColumnType("decimal(18,6)");
            entity.Property(e => e.Status)
                  .IsRequired()
                  .HasConversion(
                      v => v.ToString(),
                      v => (ServiceRequestStatus)Enum.Parse(typeof(ServiceRequestStatus), v));
            entity.Property(e => e.CreatedBy).HasMaxLength(100);

            // Index on ContractId
            entity.HasIndex(e => e.ContractId);

            // FK: ServiceRequest → Contract (restrict delete)
            entity.HasOne(e => e.Contract)
                  .WithMany(c => c.ServiceRequests)
                  .HasForeignKey(e => e.ContractId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

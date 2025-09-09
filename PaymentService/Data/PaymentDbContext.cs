using Microsoft.EntityFrameworkCore;
using PaymentService.Models.Entities;

namespace PaymentService.Data;

/// <summary>
/// Payment service database context for SQL Server
/// </summary>
public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<PaymentRefund> PaymentRefunds { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Payment entity configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasDefaultValueSql("NEWID()");
                
            entity.Property(e => e.Amount)
                .HasColumnType("money")
                .HasPrecision(18, 2);
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.HasIndex(e => e.OrderId)
                .IsUnique()
                .HasDatabaseName("IX_Payments_OrderId");
                
            entity.HasIndex(e => e.CorrelationId)
                .HasDatabaseName("IX_Payments_CorrelationId");
                
            entity.HasIndex(e => e.CustomerId)
                .HasDatabaseName("IX_Payments_CustomerId");
                
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_Payments_Status");
                
            entity.HasIndex(e => new { e.Provider, e.ProviderTransactionId })
                .HasDatabaseName("IX_Payments_Provider_TransactionId");
        });

        // PaymentMethod entity configuration
        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasDefaultValueSql("NEWID()");
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.HasIndex(e => e.CustomerId)
                .HasDatabaseName("IX_PaymentMethods_CustomerId");
                
            entity.HasIndex(e => new { e.CustomerId, e.IsDefault })
                .HasDatabaseName("IX_PaymentMethods_Customer_Default");
                
            entity.HasIndex(e => new { e.Provider, e.ProviderTokenId })
                .IsUnique()
                .HasDatabaseName("IX_PaymentMethods_Provider_Token");
        });

        // PaymentRefund entity configuration
        modelBuilder.Entity<PaymentRefund>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasDefaultValueSql("NEWID()");
                
            entity.Property(e => e.Amount)
                .HasColumnType("money")
                .HasPrecision(18, 2);
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.HasIndex(e => e.PaymentId)
                .HasDatabaseName("IX_PaymentRefunds_PaymentId");
                
            entity.HasIndex(e => e.CorrelationId)
                .HasDatabaseName("IX_PaymentRefunds_CorrelationId");
                
            entity.HasIndex(e => new { e.ProviderRefundId })
                .HasDatabaseName("IX_PaymentRefunds_ProviderRefundId");

            // Configure relationship
            entity.HasOne(e => e.Payment)
                .WithMany(e => e.Refunds)
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // You can add seed data here if needed
        // For example, default payment providers or test data
    }
    
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Payment || e.Entity is PaymentMethod || e.Entity is PaymentRefund)
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
            }
            
            entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
        }
    }
}

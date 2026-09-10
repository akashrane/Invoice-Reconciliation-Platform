using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconFlow.Core.Entities;

namespace ReconFlow.Infrastructure.Data;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
    }
}

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.AccountReference).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.AccountReference).IsUnique();
        builder.HasIndex(x => x.Name);
    }
}

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.HasIndex(x => new { x.CustomerId, x.Status });
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(x => x.Customer).WithMany(x => x.Invoices)
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TransactionReference).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.TransactionReference).IsUnique();
        builder.HasIndex(x => new { x.CustomerId, x.PaymentDate });
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.HasOne(x => x.Customer).WithMany(x => x.Payments)
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ReconciliationConfiguration : IEntityTypeConfiguration<Reconciliation>
{
    public void Configure(EntityTypeBuilder<Reconciliation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MatchStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => x.PaymentId).IsUnique();
        builder.HasIndex(x => new { x.MatchStatus, x.CreatedAt });
        builder.HasOne(x => x.Payment).WithMany(x => x.Reconciliations)
            .HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Invoice).WithMany(x => x.Reconciliations)
            .HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReviewedBy).WithMany(x => x.ReviewedReconciliations)
            .HasForeignKey(x => x.ReviewedById).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
        builder.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => new { x.ReconciliationId, x.CreatedAt });
        builder.HasOne(x => x.Reconciliation).WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.ReconciliationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

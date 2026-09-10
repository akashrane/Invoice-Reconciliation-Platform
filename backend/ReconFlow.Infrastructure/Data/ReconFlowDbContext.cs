using Microsoft.EntityFrameworkCore;
using ReconFlow.Application.Abstractions;
using ReconFlow.Core.Entities;

namespace ReconFlow.Infrastructure.Data;

public sealed class ReconFlowDbContext(DbContextOptions<ReconFlowDbContext> options)
    : DbContext(options), IReconFlowDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Reconciliation> Reconciliations => Set<Reconciliation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReconFlowDbContext).Assembly);
    }
}

using Microsoft.EntityFrameworkCore;
using ReconFlow.Core.Entities;

namespace ReconFlow.Application.Abstractions;

public interface IReconFlowDbContext
{
    DbSet<User> Users { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Reconciliation> Reconciliations { get; }
    DbSet<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

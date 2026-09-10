using Microsoft.EntityFrameworkCore;
using ReconFlow.Application.Abstractions;
using ReconFlow.Application.Contracts;
using ReconFlow.Application.Exceptions;
using ReconFlow.Core.Entities;

namespace ReconFlow.Application.Services;

public sealed class PaymentService(IReconFlowDbContext db)
{
    public async Task<IReadOnlyList<PaymentModel>> GetAllAsync(
        Guid? customerId, DateOnly? from, DateOnly? to, string? search, CancellationToken cancellationToken)
    {
        var query = db.Payments.AsNoTracking().Include(x => x.Customer).AsQueryable();
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (from.HasValue) query = query.Where(x => x.PaymentDate >= from.Value);
        if (to.HasValue) query = query.Where(x => x.PaymentDate <= to.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.TransactionReference.ToLower().Contains(term)
                || x.Description.ToLower().Contains(term)
                || x.Customer.Name.ToLower().Contains(term));
        }

        return await query.OrderByDescending(x => x.PaymentDate)
            .Select(x => new PaymentModel(x.Id, x.TransactionReference, x.CustomerId, x.Customer.Name,
                x.Amount, x.PaymentDate, x.Description, x.CreatedAt, x.Reconciliations.Any()))
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentModel> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Payments.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new PaymentModel(x.Id, x.TransactionReference, x.CustomerId, x.Customer.Name,
                x.Amount, x.PaymentDate, x.Description, x.CreatedAt, x.Reconciliations.Any()))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("PAYMENT_NOT_FOUND", $"Payment {id} could not be found.");
    }

    public async Task<PaymentModel> CreateAsync(SavePayment request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new BusinessRuleException("INVALID_AMOUNT", "Payment amount must be greater than zero.");
        if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId, cancellationToken))
            throw new NotFoundException("CUSTOMER_NOT_FOUND", $"Customer {request.CustomerId} could not be found.");

        var reference = request.TransactionReference.Trim().ToUpperInvariant();
        if (await db.Payments.AnyAsync(x => x.TransactionReference == reference, cancellationToken))
            throw new ConflictException("TRANSACTION_REFERENCE_EXISTS", $"Transaction {reference} already exists.");

        var payment = new Payment
        {
            TransactionReference = reference,
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            Description = request.Description.Trim()
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(payment.Id, cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using ReconFlow.Application.Abstractions;
using ReconFlow.Application.Contracts;
using ReconFlow.Application.Exceptions;
using ReconFlow.Core.Entities;
using ReconFlow.Core.Enums;

namespace ReconFlow.Application.Services;

public sealed class InvoiceService(IReconFlowDbContext db)
{
    public async Task<IReadOnlyList<InvoiceModel>> GetAllAsync(
        InvoiceStatus? status, Guid? customerId, DateOnly? from, DateOnly? to, string? search,
        CancellationToken cancellationToken)
    {
        var query = db.Invoices.AsNoTracking().Include(x => x.Customer).AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (from.HasValue) query = query.Where(x => x.IssueDate >= from.Value);
        if (to.HasValue) query = query.Where(x => x.IssueDate <= to.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.InvoiceNumber.ToLower().Contains(term)
                || x.Customer.Name.ToLower().Contains(term));
        }

        return await query.OrderByDescending(x => x.IssueDate)
            .Select(x => new InvoiceModel(x.Id, x.InvoiceNumber, x.CustomerId, x.Customer.Name,
                x.Amount, x.IssueDate, x.DueDate, x.Status, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<InvoiceModel> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Invoices.AsNoTracking().Include(x => x.Customer).Where(x => x.Id == id)
            .Select(x => new InvoiceModel(x.Id, x.InvoiceNumber, x.CustomerId, x.Customer.Name,
                x.Amount, x.IssueDate, x.DueDate, x.Status, x.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("INVOICE_NOT_FOUND", $"Invoice {id} could not be found.");
    }

    public async Task<InvoiceModel> CreateAsync(SaveInvoice request, CancellationToken cancellationToken)
    {
        Validate(request);
        var number = Normalize(request.InvoiceNumber);
        if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId, cancellationToken))
            throw new NotFoundException("CUSTOMER_NOT_FOUND", $"Customer {request.CustomerId} could not be found.");
        if (await db.Invoices.AnyAsync(x => x.InvoiceNumber == number, cancellationToken))
            throw new ConflictException("INVOICE_NUMBER_EXISTS", $"Invoice {number} already exists.");

        var invoice = new Invoice
        {
            InvoiceNumber = number,
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            IssueDate = request.IssueDate,
            DueDate = request.DueDate,
            Status = request.Status
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(invoice.Id, cancellationToken);
    }

    public async Task<InvoiceModel> UpdateAsync(Guid id, SaveInvoice request, CancellationToken cancellationToken)
    {
        Validate(request);
        var invoice = await db.Invoices.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("INVOICE_NOT_FOUND", $"Invoice {id} could not be found.");
        var number = Normalize(request.InvoiceNumber);
        if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId, cancellationToken))
            throw new NotFoundException("CUSTOMER_NOT_FOUND", $"Customer {request.CustomerId} could not be found.");
        if (await db.Invoices.AnyAsync(x => x.Id != id && x.InvoiceNumber == number, cancellationToken))
            throw new ConflictException("INVOICE_NUMBER_EXISTS", $"Invoice {number} already exists.");
        if (invoice.Status == InvoiceStatus.Paid && request.Status != InvoiceStatus.Paid)
            throw new BusinessRuleException("INVOICE_STATE_INVALID", "A paid invoice cannot be reopened manually.");

        invoice.InvoiceNumber = number;
        invoice.CustomerId = request.CustomerId;
        invoice.Amount = request.Amount;
        invoice.IssueDate = request.IssueDate;
        invoice.DueDate = request.DueDate;
        invoice.Status = request.Status;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private static void Validate(SaveInvoice request)
    {
        if (request.Amount <= 0) throw new BusinessRuleException("INVALID_AMOUNT", "Invoice amount must be greater than zero.");
        if (request.DueDate < request.IssueDate) throw new BusinessRuleException("INVALID_DATE_RANGE", "Due date cannot precede issue date.");
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static InvoiceModel Map(Invoice x) => new(
        x.Id, x.InvoiceNumber, x.CustomerId, x.Customer.Name, x.Amount, x.IssueDate, x.DueDate, x.Status, x.CreatedAt);
}

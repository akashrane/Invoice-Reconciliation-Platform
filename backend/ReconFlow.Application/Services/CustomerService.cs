using Microsoft.EntityFrameworkCore;
using ReconFlow.Application.Abstractions;
using ReconFlow.Application.Contracts;
using ReconFlow.Application.Exceptions;
using ReconFlow.Core.Entities;

namespace ReconFlow.Application.Services;

public sealed class CustomerService(IReconFlowDbContext db)
{
    public async Task<IReadOnlyList<CustomerModel>> GetAllAsync(string? search, CancellationToken cancellationToken)
    {
        var query = db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term)
                || x.Email.ToLower().Contains(term)
                || x.AccountReference.ToLower().Contains(term));
        }

        return await query.OrderBy(x => x.Name)
            .Select(x => new CustomerModel(x.Id, x.Name, x.Email, x.AccountReference, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerModel> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Customers.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new CustomerModel(x.Id, x.Name, x.Email, x.AccountReference, x.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("CUSTOMER_NOT_FOUND", $"Customer {id} could not be found.");
    }

    public async Task<CustomerModel> CreateAsync(SaveCustomer request, CancellationToken cancellationToken)
    {
        var reference = Normalize(request.AccountReference);
        if (await db.Customers.AnyAsync(x => x.AccountReference == reference, cancellationToken))
            throw new ConflictException("CUSTOMER_REFERENCE_EXISTS", $"Account reference {reference} already exists.");

        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            AccountReference = reference
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);
        return Map(customer);
    }

    public async Task<CustomerModel> UpdateAsync(Guid id, SaveCustomer request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("CUSTOMER_NOT_FOUND", $"Customer {id} could not be found.");
        var reference = Normalize(request.AccountReference);
        if (await db.Customers.AnyAsync(x => x.Id != id && x.AccountReference == reference, cancellationToken))
            throw new ConflictException("CUSTOMER_REFERENCE_EXISTS", $"Account reference {reference} already exists.");

        customer.Name = request.Name.Trim();
        customer.Email = request.Email.Trim().ToLowerInvariant();
        customer.AccountReference = reference;
        await db.SaveChangesAsync(cancellationToken);
        return Map(customer);
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static CustomerModel Map(Customer x) => new(x.Id, x.Name, x.Email, x.AccountReference, x.CreatedAt);
}

using Microsoft.EntityFrameworkCore;
using ReconFlow.Application.Contracts;
using ReconFlow.Application.Exceptions;
using ReconFlow.Application.Services;
using ReconFlow.Core.Entities;
using ReconFlow.Core.Enums;
using ReconFlow.Core.Services;
using ReconFlow.Infrastructure.Data;

namespace ReconFlow.UnitTests;

public sealed class ReconciliationServiceTests
{
    [Fact]
    public async Task Exact_match_is_auto_matched_and_marks_invoice_paid()
    {
        await using var db = NewDb();
        var fixture = await SeedAsync(db, "Invoice INV-102");

        var result = await Service(db).ReconcilePaymentAsync(fixture.Payment.Id, default);

        Assert.Equal(ReconciliationStatus.Matched, result.MatchStatus);
        Assert.Equal(100, result.MatchScore);
        Assert.Equal(InvoiceStatus.Paid, (await db.Invoices.FindAsync(fixture.Invoice.Id))!.Status);
    }

    [Fact]
    public async Task Amount_match_without_reference_enters_review()
    {
        await using var db = NewDb();
        var fixture = await SeedAsync(db, "Account payment");

        var result = await Service(db).ReconcilePaymentAsync(fixture.Payment.Id, default);

        Assert.Equal(ReconciliationStatus.Review, result.MatchStatus);
        Assert.Equal(60, result.MatchScore);
        Assert.Equal(InvoiceStatus.Open, fixture.Invoice.Status);
    }

    [Fact]
    public async Task Wrong_customer_cannot_cross_match()
    {
        await using var db = NewDb();
        var fixture = await SeedAsync(db, "Invoice INV-102");
        var otherCustomer = new Customer
        {
            Name = "XYZ Inc",
            Email = "billing@xyz.test",
            AccountReference = "XYZ-001"
        };
        db.Customers.Add(otherCustomer);
        fixture.Payment.Customer = otherCustomer;
        fixture.Payment.CustomerId = otherCustomer.Id;
        await db.SaveChangesAsync();

        var result = await Service(db).ReconcilePaymentAsync(fixture.Payment.Id, default);

        Assert.Equal(ReconciliationStatus.Unmatched, result.MatchStatus);
        Assert.Null(result.InvoiceId);
    }

    [Fact]
    public async Task Paid_invoice_is_not_a_candidate()
    {
        await using var db = NewDb();
        var fixture = await SeedAsync(db, "Invoice INV-102");
        fixture.Invoice.Status = InvoiceStatus.Paid;
        await db.SaveChangesAsync();

        var result = await Service(db).ReconcilePaymentAsync(fixture.Payment.Id, default);

        Assert.Equal(ReconciliationStatus.Unmatched, result.MatchStatus);
    }

    [Fact]
    public async Task Payment_cannot_be_reconciled_twice()
    {
        await using var db = NewDb();
        var fixture = await SeedAsync(db, "Account payment");
        await Service(db).ReconcilePaymentAsync(fixture.Payment.Id, default);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            Service(db).ReconcilePaymentAsync(fixture.Payment.Id, default));

        Assert.Equal("PAYMENT_ALREADY_RECONCILED", exception.Code);
    }

    [Fact]
    public async Task Duplicate_transaction_reference_is_rejected()
    {
        await using var db = NewDb();
        var fixture = await SeedAsync(db, "Account payment");

        var exception = await Assert.ThrowsAsync<ConflictException>(() => new PaymentService(db).CreateAsync(
            new SavePayment(fixture.Payment.TransactionReference, fixture.Customer.Id, 10m, fixture.Payment.PaymentDate, "Duplicate"), default));

        Assert.Equal("TRANSACTION_REFERENCE_EXISTS", exception.Code);
    }

    [Fact]
    public async Task Review_can_be_approved_and_is_audited()
    {
        await using var db = NewDb();
        var fixture = await SeedAsync(db, "Account payment");
        var service = Service(db);
        var review = await service.ReconcilePaymentAsync(fixture.Payment.Id, default);

        var approved = await service.ApproveReconciliationAsync(review.Id, fixture.User.Id, "Verified remittance advice", default);

        Assert.Equal(ReconciliationStatus.Matched, approved.MatchStatus);
        Assert.Equal(InvoiceStatus.Paid, (await db.Invoices.FindAsync(fixture.Invoice.Id))!.Status);
        Assert.Single(approved.AuditTrail);
        Assert.Equal("APPROVE", approved.AuditTrail[0].Action);
    }

    [Fact]
    public async Task Review_can_be_rejected_without_paying_invoice()
    {
        await using var db = NewDb();
        var fixture = await SeedAsync(db, "Account payment");
        var service = Service(db);
        var review = await service.ReconcilePaymentAsync(fixture.Payment.Id, default);

        var rejected = await service.RejectReconciliationAsync(review.Id, fixture.User.Id, "Payment belongs to another invoice", default);

        Assert.Equal(ReconciliationStatus.Rejected, rejected.MatchStatus);
        Assert.Equal(InvoiceStatus.Open, (await db.Invoices.FindAsync(fixture.Invoice.Id))!.Status);
        Assert.Equal("REJECT", rejected.AuditTrail.Single().Action);
    }

    private static ReconFlowDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ReconFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReconFlowDbContext(options);
    }

    private static ReconciliationService Service(ReconFlowDbContext db) => new(db, new MatchingRulesEngine());

    private static async Task<Fixture> SeedAsync(ReconFlowDbContext db, string description)
    {
        var customer = new Customer { Name = "ABC Corp", Email = "billing@abc.test", AccountReference = "ABC-001" };
        var user = new User { Email = "analyst@reconflow.test", PasswordHash = "hash", Role = UserRole.Analyst };
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-102",
            Customer = customer,
            CustomerId = customer.Id,
            Amount = 1250m,
            IssueDate = new DateOnly(2026, 8, 1),
            DueDate = new DateOnly(2026, 9, 1)
        };
        var payment = new Payment
        {
            TransactionReference = "TX-19202",
            Customer = customer,
            CustomerId = customer.Id,
            Amount = 1250m,
            PaymentDate = new DateOnly(2026, 9, 1),
            Description = description
        };
        db.AddRange(customer, user, invoice, payment);
        await db.SaveChangesAsync();
        return new Fixture(customer, user, invoice, payment);
    }

    private sealed record Fixture(Customer Customer, User User, Invoice Invoice, Payment Payment);
}

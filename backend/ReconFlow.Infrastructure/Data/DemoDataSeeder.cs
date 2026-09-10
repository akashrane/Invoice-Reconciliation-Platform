using Microsoft.EntityFrameworkCore;
using ReconFlow.Application.Abstractions;
using ReconFlow.Core.Entities;
using ReconFlow.Core.Enums;

namespace ReconFlow.Infrastructure.Data;

public sealed class DemoDataSeeder(ReconFlowDbContext db, IPasswordService passwords)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Customers.AnyAsync(cancellationToken)) return;

        var analyst = new User
        {
            Email = "analyst@reconflow.dev",
            PasswordHash = passwords.Hash("ReconFlow!2026"),
            Role = UserRole.Analyst
        };
        var admin = new User
        {
            Email = "admin@reconflow.dev",
            PasswordHash = passwords.Hash("ReconFlow!2026"),
            Role = UserRole.Admin
        };
        var abc = new Customer { Name = "ABC Corporation", Email = "ar@abc-corp.example", AccountReference = "ABC-001" };
        var xyz = new Customer { Name = "XYZ Industries", Email = "finance@xyz.example", AccountReference = "XYZ-042" };
        var northstar = new Customer { Name = "Northstar Labs", Email = "billing@northstar.example", AccountReference = "NSL-117" };

        var paidInvoice = Invoice("INV-1002", abc, 980m, new(2026, 7, 12), new(2026, 8, 12), InvoiceStatus.Paid);
        var openInvoice = Invoice("INV-102", abc, 1250m, new(2026, 8, 15), new(2026, 9, 15), InvoiceStatus.Open);
        var reviewInvoice = Invoice("INV-120", abc, 1200m, new(2026, 8, 20), new(2026, 9, 20), InvoiceStatus.Open);
        var overdueInvoice = Invoice("INV-204", xyz, 860m, new(2026, 7, 1), new(2026, 8, 1), InvoiceStatus.Overdue);
        var rejectedInvoice = Invoice("INV-310", northstar, 2500m, new(2026, 8, 10), new(2026, 9, 10), InvoiceStatus.Open);

        var matchedPayment = Payment("TX-19001", abc, 980m, new(2026, 8, 10), "Settlement for INV-1002");
        var reviewPayment = Payment("TX-19203", abc, 1200m, new(2026, 9, 2), "September account payment");
        var unmatchedPayment = Payment("TX-19204", xyz, 430m, new(2026, 9, 3), "Partial transfer");
        var rejectedPayment = Payment("TX-19205", northstar, 2500m, new(2026, 9, 5), "Wire transfer");

        var matched = Reconciliation(matchedPayment, paidInvoice, 100, ReconciliationStatus.Matched,
            "Exact amount matched (+40)|Invoice reference detected (+40)|Customer matched (+15)|Payment date within reconciliation window (+5)");
        var review = Reconciliation(reviewPayment, reviewInvoice, 60, ReconciliationStatus.Review,
            "Exact amount matched (+40)|Invoice reference not detected (+0)|Customer matched (+15)|Payment date within reconciliation window (+5)");
        var unmatched = Reconciliation(unmatchedPayment, overdueInvoice, 20, ReconciliationStatus.Unmatched,
            "Amount did not match (+0)|Invoice reference not detected (+0)|Customer matched (+15)|Payment date within reconciliation window (+5)");
        var rejected = Reconciliation(rejectedPayment, rejectedInvoice, 60, ReconciliationStatus.Rejected,
            "Exact amount matched (+40)|Invoice reference not detected (+0)|Customer matched (+15)|Payment date within reconciliation window (+5)");
        rejected.ReviewedBy = analyst;
        rejected.ReviewedById = analyst.Id;
        rejected.ReviewedAt = new DateTimeOffset(2026, 9, 9, 14, 21, 0, TimeSpan.Zero);
        var audit = new AuditLog
        {
            Reconciliation = rejected,
            ReconciliationId = rejected.Id,
            User = analyst,
            UserId = analyst.Id,
            Action = "REJECT",
            PreviousStatus = ReconciliationStatus.Review,
            NewStatus = ReconciliationStatus.Rejected,
            PaymentId = rejectedPayment.Id,
            InvoiceId = rejectedInvoice.Id,
            Reason = "Reference mismatch confirmed with treasury",
            CreatedAt = rejected.ReviewedAt.Value
        };

        db.AddRange(analyst, admin, abc, xyz, northstar, paidInvoice, openInvoice, reviewInvoice,
            overdueInvoice, rejectedInvoice, matchedPayment, reviewPayment, unmatchedPayment, rejectedPayment,
            matched, review, unmatched, rejected, audit);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static Invoice Invoice(string number, Customer customer, decimal amount, DateOnly issue, DateOnly due, InvoiceStatus status) => new()
    {
        InvoiceNumber = number,
        Customer = customer,
        CustomerId = customer.Id,
        Amount = amount,
        IssueDate = issue,
        DueDate = due,
        Status = status
    };

    private static Payment Payment(string reference, Customer customer, decimal amount, DateOnly date, string description) => new()
    {
        TransactionReference = reference,
        Customer = customer,
        CustomerId = customer.Id,
        Amount = amount,
        PaymentDate = date,
        Description = description
    };

    private static Reconciliation Reconciliation(Payment payment, Invoice invoice, int score, ReconciliationStatus status, string reason) => new()
    {
        Payment = payment,
        PaymentId = payment.Id,
        Invoice = invoice,
        InvoiceId = invoice.Id,
        MatchScore = score,
        MatchStatus = status,
        Reason = reason
    };
}

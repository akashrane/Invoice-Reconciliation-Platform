using System.Text;
using Microsoft.EntityFrameworkCore;
using ReconFlow.Application.Services;
using ReconFlow.Core.Entities;
using ReconFlow.Core.Services;
using ReconFlow.Infrastructure.Data;

namespace ReconFlow.UnitTests;

public sealed class CsvPaymentImportServiceTests
{
    [Fact]
    public async Task Import_continues_after_invalid_row_and_returns_summary()
    {
        await using var db = NewDb();
        var customerA = new Customer { Name = "ABC Corp", Email = "a@test.dev", AccountReference = "ABC-001" };
        var customerB = new Customer { Name = "XYZ Inc", Email = "b@test.dev", AccountReference = "XYZ-001" };
        db.Customers.AddRange(customerA, customerB);
        db.Invoices.Add(new Invoice
        {
            InvoiceNumber = "INV-102", Customer = customerA, CustomerId = customerA.Id, Amount = 1250m,
            IssueDate = new DateOnly(2026, 8, 1), DueDate = new DateOnly(2026, 9, 15)
        });
        await db.SaveChangesAsync();
        var csv = """
            reference,customer,amount,date,description
            TX100,ABC Corp,1250.00,2026-09-01,Invoice INV-102
            TX101,ABC Corp,not-money,2026-09-02,Bad row
            TX102,XYZ Inc,430.00,2026-09-02,Payment
            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var reconciler = new ReconciliationService(db, new MatchingRulesEngine());

        var result = await new CsvPaymentImportService(db, reconciler).ImportAsync(stream, default);

        Assert.Equal(3, result.Uploaded);
        Assert.Equal(1, result.Matched);
        Assert.Equal(1, result.Unmatched);
        Assert.Equal(1, result.Failed);
        Assert.Single(result.Errors);
        Assert.Equal(2, await db.Payments.CountAsync());
    }

    [Fact]
    public async Task Missing_header_is_reported_without_importing_rows()
    {
        await using var db = NewDb();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("reference,amount\nTX1,10"));
        var reconciler = new ReconciliationService(db, new MatchingRulesEngine());

        var result = await new CsvPaymentImportService(db, reconciler).ImportAsync(stream, default);

        Assert.Equal(0, result.Uploaded);
        Assert.Equal(1, result.Failed);
        Assert.Contains("Missing required columns", result.Errors.Single().Message);
    }

    private static ReconFlowDbContext NewDb() => new(new DbContextOptionsBuilder<ReconFlowDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
}

using ReconFlow.Core.Entities;
using ReconFlow.Core.Services;

namespace ReconFlow.UnitTests;

public sealed class MatchingRulesEngineTests
{
    private readonly MatchingRulesEngine _engine = new();

    [Fact]
    public void Exact_amount_reference_customer_and_date_scores_100()
    {
        var customerId = Guid.NewGuid();
        var result = _engine.Evaluate(Payment(customerId, 1250m, "Payment for INV-102"), Invoice(customerId));

        Assert.Equal(100, result.Score);
        Assert.Contains(result.Reasons, x => x.StartsWith("Exact amount"));
        Assert.Contains(result.Reasons, x => x.StartsWith("Invoice reference detected"));
    }

    [Fact]
    public void Amount_without_reference_scores_review_threshold()
    {
        var customerId = Guid.NewGuid();
        var result = _engine.Evaluate(Payment(customerId, 1250m, "Account payment"), Invoice(customerId));

        Assert.Equal(60, result.Score);
    }

    private static Invoice Invoice(Guid customerId) => new()
    {
        InvoiceNumber = "INV-102",
        CustomerId = customerId,
        Amount = 1250m,
        IssueDate = new DateOnly(2026, 8, 1),
        DueDate = new DateOnly(2026, 9, 1)
    };

    private static Payment Payment(Guid customerId, decimal amount, string description) => new()
    {
        TransactionReference = "TX-100",
        CustomerId = customerId,
        Amount = amount,
        PaymentDate = new DateOnly(2026, 9, 1),
        Description = description
    };
}

using ReconFlow.Core.Entities;

namespace ReconFlow.Core.Services;

public sealed record MatchEvaluation(Guid InvoiceId, int Score, IReadOnlyList<string> Reasons)
{
    public string Explanation => string.Join('|', Reasons);
}

public interface IMatchingRulesEngine
{
    MatchEvaluation Evaluate(Payment payment, Invoice invoice);
}

public sealed class MatchingRulesEngine : IMatchingRulesEngine
{
    public MatchEvaluation Evaluate(Payment payment, Invoice invoice)
    {
        var score = 0;
        var reasons = new List<string>(4);

        if (payment.Amount == invoice.Amount)
        {
            score += 40;
            reasons.Add("Exact amount matched (+40)");
        }
        else reasons.Add("Amount did not match (+0)");

        var referenceFound = ContainsInvoiceReference(payment, invoice.InvoiceNumber);
        if (referenceFound)
        {
            score += 40;
            reasons.Add("Invoice reference detected (+40)");
        }
        else reasons.Add("Invoice reference not detected (+0)");

        if (payment.CustomerId == invoice.CustomerId)
        {
            score += 15;
            reasons.Add("Customer matched (+15)");
        }
        else reasons.Add("Customer did not match (+0)");

        var windowStart = invoice.IssueDate.AddDays(-7);
        var windowEnd = invoice.DueDate.AddDays(30);
        if (payment.PaymentDate >= windowStart && payment.PaymentDate <= windowEnd)
        {
            score += 5;
            reasons.Add("Payment date within reconciliation window (+5)");
        }
        else reasons.Add("Payment date outside reconciliation window (+0)");

        return new MatchEvaluation(invoice.Id, score, reasons);
    }

    private static bool ContainsInvoiceReference(Payment payment, string invoiceNumber)
    {
        return payment.Description.Contains(invoiceNumber, StringComparison.OrdinalIgnoreCase)
            || payment.TransactionReference.Contains(invoiceNumber, StringComparison.OrdinalIgnoreCase);
    }
}

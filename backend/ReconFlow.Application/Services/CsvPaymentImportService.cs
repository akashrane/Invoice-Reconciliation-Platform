using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using ReconFlow.Application.Abstractions;
using ReconFlow.Application.Contracts;
using ReconFlow.Core.Entities;
using ReconFlow.Core.Enums;

namespace ReconFlow.Application.Services;

public sealed class CsvPaymentImportService(IReconFlowDbContext db, ReconciliationService reconciliationService)
{
    private static readonly string[] RequiredHeaders = ["reference", "customer", "amount", "date", "description"];

    public async Task<PaymentImportResult> ImportAsync(Stream stream, CancellationToken cancellationToken)
    {
        var errors = new List<PaymentImportError>();
        var uploaded = 0;
        var matched = 0;
        var review = 0;
        var unmatched = 0;
        var failed = 0;

        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        });

        if (!await csv.ReadAsync() || !csv.ReadHeader())
            return new PaymentImportResult(0, 0, 0, 0, 1, [new(1, null, "The CSV header is missing.")]);

        var headers = csv.HeaderRecord?.Select(x => x.Trim().ToLowerInvariant()).ToHashSet() ?? [];
        var missing = RequiredHeaders.Where(x => !headers.Contains(x)).ToArray();
        if (missing.Length > 0)
            return new PaymentImportResult(0, 0, 0, 0, 1,
                [new(1, null, $"Missing required columns: {string.Join(", ", missing)}.")]);

        var customers = await db.Customers.AsNoTracking().ToListAsync(cancellationToken);
        var byReference = customers.ToDictionary(x => x.AccountReference, StringComparer.OrdinalIgnoreCase);
        var byName = customers.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var transactionReferences = await db.Payments.AsNoTracking()
            .Select(x => x.TransactionReference).ToHashSetAsync(cancellationToken);

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            uploaded++;
            var rowNumber = csv.Parser.Row;
            string? reference = null;
            try
            {
                reference = Required(csv, "reference").ToUpperInvariant();
                var customerValue = Required(csv, "customer");
                var amountValue = Required(csv, "amount");
                var dateValue = Required(csv, "date");
                var description = Required(csv, "description");

                if (!decimal.TryParse(amountValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
                    throw new FormatException("Amount must be a positive decimal number.");
                if (!DateOnly.TryParseExact(dateValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    throw new FormatException("Date must use YYYY-MM-DD format.");
                if (!byReference.TryGetValue(customerValue, out var customer) && !byName.TryGetValue(customerValue, out customer))
                    throw new FormatException($"Customer '{customerValue}' could not be resolved.");
                if (!transactionReferences.Add(reference))
                    throw new FormatException($"Transaction reference {reference} already exists.");

                var payment = new Payment
                {
                    TransactionReference = reference,
                    CustomerId = customer.Id,
                    Amount = amount,
                    PaymentDate = date,
                    Description = description
                };
                db.Payments.Add(payment);
                await db.SaveChangesAsync(cancellationToken);
                var reconciliation = await reconciliationService.ReconcilePaymentAsync(payment.Id, cancellationToken);
                switch (reconciliation.MatchStatus)
                {
                    case ReconciliationStatus.Matched: matched++; break;
                    case ReconciliationStatus.Review: review++; break;
                    default: unmatched++; break;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failed++;
                errors.Add(new PaymentImportError(rowNumber, reference, exception.Message));
            }
        }

        return new PaymentImportResult(uploaded, matched, review, unmatched, failed, errors);
    }

    private static string Required(CsvReader csv, string name)
    {
        var value = csv.GetField(name)?.Trim();
        return !string.IsNullOrWhiteSpace(value) ? value : throw new FormatException($"Column '{name}' is required.");
    }
}

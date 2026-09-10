using Microsoft.EntityFrameworkCore;
using ReconFlow.Application.Abstractions;
using ReconFlow.Application.Contracts;
using ReconFlow.Application.Exceptions;
using ReconFlow.Core.Entities;
using ReconFlow.Core.Enums;
using ReconFlow.Core.Services;

namespace ReconFlow.Application.Services;

public sealed class ReconciliationService(IReconFlowDbContext db, IMatchingRulesEngine matchingEngine)
{
    public async Task<ReconciliationModel> ReconcilePaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.Include(x => x.Customer)
            .SingleOrDefaultAsync(x => x.Id == paymentId, cancellationToken)
            ?? throw new NotFoundException("PAYMENT_NOT_FOUND", $"Payment {paymentId} could not be found.");
        if (await db.Reconciliations.AnyAsync(x => x.PaymentId == paymentId, cancellationToken))
            throw new ConflictException("PAYMENT_ALREADY_RECONCILED", $"Payment {payment.TransactionReference} has already been reconciled.");

        var candidates = await FindCandidateInvoicesAsync(payment, cancellationToken);
        Reconciliation reconciliation;
        if (candidates.Count == 0)
        {
            reconciliation = new Reconciliation
            {
                PaymentId = payment.Id,
                MatchScore = 0,
                MatchStatus = ReconciliationStatus.Unmatched,
                Reason = "No eligible invoices for this customer"
            };
        }
        else
        {
            var evaluations = candidates.Select(invoice => new
                {
                    Invoice = invoice,
                    Evaluation = matchingEngine.Evaluate(payment, invoice)
                })
                .OrderByDescending(x => x.Evaluation.Score)
                .ThenBy(x => x.Invoice.DueDate)
                .ToList();
            var best = evaluations[0];
            var status = Classify(best.Evaluation.Score);
            reconciliation = new Reconciliation
            {
                PaymentId = payment.Id,
                InvoiceId = best.Invoice.Id,
                MatchScore = best.Evaluation.Score,
                MatchStatus = status,
                Reason = best.Evaluation.Explanation
            };
            if (status == ReconciliationStatus.Matched)
                best.Invoice.Status = InvoiceStatus.Paid;
        }

        db.Reconciliations.Add(reconciliation);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(reconciliation.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> FindCandidateInvoicesAsync(Payment payment, CancellationToken cancellationToken)
    {
        return await db.Invoices.Where(x => x.CustomerId == payment.CustomerId && x.Status != InvoiceStatus.Paid)
            .ToListAsync(cancellationToken);
    }

    public MatchEvaluation CalculateMatchScore(Payment payment, Invoice invoice) => matchingEngine.Evaluate(payment, invoice);

    public async Task<IReadOnlyList<ReconciliationModel>> GetAllAsync(
        ReconciliationStatus? status, CancellationToken cancellationToken)
    {
        var query = BuildQuery();
        if (status.HasValue) query = query.Where(x => x.MatchStatus == status.Value);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public Task<IReadOnlyList<ReconciliationModel>> GetReviewQueueAsync(CancellationToken cancellationToken) =>
        GetAllAsync(ReconciliationStatus.Review, cancellationToken);

    public async Task<ReconciliationModel> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await BuildQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("RECONCILIATION_NOT_FOUND", $"Reconciliation {id} could not be found.");
        return Map(row);
    }

    public Task<ReconciliationModel> ApproveReconciliationAsync(
        Guid id, Guid reviewerId, string reason, CancellationToken cancellationToken) =>
        ReviewAsync(id, reviewerId, ReconciliationStatus.Matched, reason, cancellationToken);

    public Task<ReconciliationModel> RejectReconciliationAsync(
        Guid id, Guid reviewerId, string reason, CancellationToken cancellationToken) =>
        ReviewAsync(id, reviewerId, ReconciliationStatus.Rejected, reason, cancellationToken);

    private async Task<ReconciliationModel> ReviewAsync(
        Guid id, Guid reviewerId, ReconciliationStatus target, string reason, CancellationToken cancellationToken)
    {
        var reconciliation = await db.Reconciliations.Include(x => x.Invoice)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("RECONCILIATION_NOT_FOUND", $"Reconciliation {id} could not be found.");
        if (reconciliation.MatchStatus != ReconciliationStatus.Review)
            throw new BusinessRuleException("RECONCILIATION_STATE_INVALID", "Only reconciliations in review can be approved or rejected.");
        if (!await db.Users.AnyAsync(x => x.Id == reviewerId, cancellationToken))
            throw new NotFoundException("USER_NOT_FOUND", "The reviewing user could not be found.");
        if (target == ReconciliationStatus.Matched && reconciliation.Invoice is null)
            throw new BusinessRuleException("INVOICE_REQUIRED", "A reconciliation cannot be approved without an invoice.");
        if (target == ReconciliationStatus.Matched && reconciliation.Invoice!.Status == InvoiceStatus.Paid)
            throw new ConflictException("INVOICE_ALREADY_PAID", $"Invoice {reconciliation.Invoice.InvoiceNumber} is already paid.");

        var previous = reconciliation.MatchStatus;
        reconciliation.MatchStatus = target;
        reconciliation.ReviewedById = reviewerId;
        reconciliation.ReviewedAt = DateTimeOffset.UtcNow;
        if (target == ReconciliationStatus.Matched) reconciliation.Invoice!.Status = InvoiceStatus.Paid;

        db.AuditLogs.Add(new AuditLog
        {
            ReconciliationId = reconciliation.Id,
            UserId = reviewerId,
            Action = target == ReconciliationStatus.Matched ? "APPROVE" : "REJECT",
            PreviousStatus = previous,
            NewStatus = target,
            PaymentId = reconciliation.PaymentId,
            InvoiceId = reconciliation.InvoiceId,
            Reason = reason.Trim()
        });
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private IQueryable<Reconciliation> BuildQuery() => db.Reconciliations.AsNoTracking()
        .Include(x => x.Payment).ThenInclude(x => x.Customer)
        .Include(x => x.Invoice)
        .Include(x => x.ReviewedBy)
        .Include(x => x.AuditLogs).ThenInclude(x => x.User);

    private static ReconciliationStatus Classify(int score) => score switch
    {
        >= 90 => ReconciliationStatus.Matched,
        >= 60 => ReconciliationStatus.Review,
        _ => ReconciliationStatus.Unmatched
    };

    private static ReconciliationModel Map(Reconciliation x) => new(
        x.Id,
        x.PaymentId,
        x.Payment.TransactionReference,
        x.InvoiceId,
        x.Invoice?.InvoiceNumber,
        x.Payment.Customer.Name,
        x.Payment.Amount,
        x.MatchScore,
        x.MatchStatus,
        x.Reason.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        x.ReviewedBy?.Email,
        x.CreatedAt,
        x.ReviewedAt,
        x.AuditLogs.OrderByDescending(a => a.CreatedAt)
            .Select(a => new AuditModel(a.Id, a.User.Email, a.Action, a.PreviousStatus, a.NewStatus, a.Reason, a.CreatedAt))
            .ToList());
}

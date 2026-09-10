using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReconFlow.Api.DTOs;
using ReconFlow.Application.Contracts;
using ReconFlow.Application.Services;
using ReconFlow.Core.Enums;

namespace ReconFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "Analyst,Admin")]
[Route("api/reconciliation")]
public sealed class ReconciliationController(ReconciliationService service) : ControllerBase
{
    [HttpPost("run/{paymentId:guid}")]
    public async Task<ActionResult<ReconciliationModel>> Run(Guid paymentId, CancellationToken ct) =>
        Ok(await service.ReconcilePaymentAsync(paymentId, ct));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReconciliationModel>>> GetAll(
        [FromQuery] ReconciliationStatus? status, CancellationToken ct) => Ok(await service.GetAllAsync(status, ct));

    [HttpGet("review")]
    public async Task<ActionResult<IReadOnlyList<ReconciliationModel>>> ReviewQueue(CancellationToken ct) =>
        Ok(await service.GetReviewQueueAsync(ct));

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ReconciliationModel>> Approve(Guid id, ReviewDecisionRequest request, CancellationToken ct) =>
        Ok(await service.ApproveReconciliationAsync(id, CurrentUserId(), request.Reason, ct));

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ReconciliationModel>> Reject(Guid id, ReviewDecisionRequest request, CancellationToken ct) =>
        Ok(await service.RejectReconciliationAsync(id, CurrentUserId(), request.Reason, ct));

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException("Authenticated user id is unavailable.");
    }
}

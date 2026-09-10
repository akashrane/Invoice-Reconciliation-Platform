using Microsoft.AspNetCore.Mvc;
using ReconFlow.Api.DTOs;
using ReconFlow.Application.Contracts;
using ReconFlow.Application.Services;

namespace ReconFlow.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(PaymentService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentModel>>> GetAll(
        [FromQuery] Guid? customer, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] string? search, CancellationToken ct) =>
        Ok(await service.GetAllAsync(customer, from, to, search, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentModel>> Get(Guid id, CancellationToken ct) => Ok(await service.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<PaymentModel>> Create(SavePaymentRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(new(request.TransactionReference, request.CustomerId,
            request.Amount, request.PaymentDate, request.Description), ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }
}

using Microsoft.AspNetCore.Mvc;
using ReconFlow.Api.DTOs;
using ReconFlow.Application.Contracts;
using ReconFlow.Application.Services;
using ReconFlow.Core.Enums;

namespace ReconFlow.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController(InvoiceService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceModel>>> GetAll(
        [FromQuery] InvoiceStatus? status, [FromQuery] Guid? customer,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? search,
        CancellationToken ct) => Ok(await service.GetAllAsync(status, customer, from, to, search, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceModel>> Get(Guid id, CancellationToken ct) => Ok(await service.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<InvoiceModel>> Create(SaveInvoiceRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(Map(request), ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvoiceModel>> Update(Guid id, SaveInvoiceRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, Map(request), ct));

    private static SaveInvoice Map(SaveInvoiceRequest request) => new(
        request.InvoiceNumber, request.CustomerId, request.Amount, request.IssueDate, request.DueDate, request.Status);
}

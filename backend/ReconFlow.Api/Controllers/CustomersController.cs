using Microsoft.AspNetCore.Mvc;
using ReconFlow.Api.DTOs;
using ReconFlow.Application.Contracts;
using ReconFlow.Application.Services;

namespace ReconFlow.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(CustomerService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerModel>>> GetAll([FromQuery] string? search, CancellationToken ct) =>
        Ok(await service.GetAllAsync(search, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerModel>> Get(Guid id, CancellationToken ct) => Ok(await service.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CustomerModel>> Create(SaveCustomerRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(new(request.Name, request.Email, request.AccountReference), ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerModel>> Update(Guid id, SaveCustomerRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, new(request.Name, request.Email, request.AccountReference), ct));
}

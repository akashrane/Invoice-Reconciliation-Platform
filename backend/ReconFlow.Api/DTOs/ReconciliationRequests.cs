using System.ComponentModel.DataAnnotations;

namespace ReconFlow.Api.DTOs;

public sealed record ReviewDecisionRequest(
    [property: Required, StringLength(1000, MinimumLength = 3)] string Reason);

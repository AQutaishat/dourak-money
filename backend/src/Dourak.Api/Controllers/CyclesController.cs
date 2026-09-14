using Dourak.Application.Circles.Commands;
using Dourak.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dourak.Api.Controllers;

public record RecordContributionRequest(int MemberId, decimal PaidAmount, DateTimeOffset? PaidAt, PaymentMethod? PaymentMethod, string? Notes);
public record RecordPayoutRequest(decimal ActualAmount, DateTimeOffset? PaidAt, PaymentMethod? PaymentMethod, string? Notes);

/// <summary>
/// Operations scoped to one cycle (contribution recording, payout recording).
/// Ownership is verified inside the handlers by walking Cycle -> Circle -> OrganizerUserId.
/// </summary>
[ApiController]
[Authorize]
[Route("api/cycles")]
public class CyclesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CyclesController(IMediator mediator) => _mediator = mediator;

    [HttpPost("{cycleId:int}/contributions")]
    public async Task<IActionResult> RecordContribution(int cycleId, RecordContributionRequest request)
    {
        await _mediator.Send(new RecordContributionCommand(
            cycleId, request.MemberId, request.PaidAmount, request.PaidAt, request.PaymentMethod, request.Notes));
        return NoContent();
    }

    [HttpPost("{cycleId:int}/payout")]
    public async Task<IActionResult> RecordPayout(int cycleId, RecordPayoutRequest request)
    {
        await _mediator.Send(new RecordPayoutCommand(cycleId, request.ActualAmount, request.PaidAt, request.PaymentMethod, request.Notes));
        return NoContent();
    }

    [HttpPost("{cycleId:int}/payout/reopen")]
    public async Task<IActionResult> ReopenPayout(int cycleId)
    {
        await _mediator.Send(new ReopenPayoutCommand(cycleId));
        return NoContent();
    }
}

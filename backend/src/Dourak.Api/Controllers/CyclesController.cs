using Dourak.Application.Circles.Commands;
using Dourak.Application.Circles.Queries;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dourak.Api.Controllers;

public record RecordContributionRequest(int MemberId, decimal PaidAmount, DateTimeOffset? PaidAt, PaymentMethod? PaymentMethod, string? Notes);

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

    /// <summary>Multipart so an evidence file (e.g. a transfer screenshot) can ride along, same as a payment claim.</summary>
    [HttpPost("{cycleId:int}/payout")]
    [RequestSizeLimit(IEvidenceFileStorage.MaxSizeBytes + 512 * 1024)]
    public async Task<IActionResult> RecordPayout(
        int cycleId,
        [FromForm] decimal actualAmount,
        [FromForm] DateTimeOffset? paidAt,
        [FromForm] PaymentMethod? paymentMethod,
        [FromForm] string? notes,
        IFormFile? evidence)
    {
        EvidenceUpload? upload = null;
        Stream? stream = null;
        try
        {
            if (evidence is not null && evidence.Length > 0)
            {
                stream = evidence.OpenReadStream();
                upload = new EvidenceUpload(evidence.FileName, evidence.ContentType ?? "application/octet-stream", evidence.Length, stream);
            }

            await _mediator.Send(new RecordPayoutCommand(cycleId, actualAmount, paidAt, paymentMethod, notes, upload));
            return NoContent();
        }
        finally
        {
            if (stream is not null) await stream.DisposeAsync();
        }
    }

    [HttpPost("{cycleId:int}/payout/reopen")]
    public async Task<IActionResult> ReopenPayout(int cycleId)
    {
        await _mediator.Send(new ReopenPayoutCommand(cycleId));
        return NoContent();
    }

    [HttpGet("payout-payments/{payoutPaymentId:int}/evidence")]
    public async Task<IActionResult> GetPayoutEvidence(int payoutPaymentId)
    {
        var download = await _mediator.Send(new GetPayoutPaymentEvidenceQuery(payoutPaymentId));
        if (download is null) return NotFound();
        return File(download.Content, download.ContentType, download.FileName);
    }
}

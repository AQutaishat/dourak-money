using Dourak.Application.Circles.Commands;
using Dourak.Application.Circles.Dtos;
using Dourak.Application.Circles.Queries;
using Dourak.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dourak.Api.Controllers;

/// <summary>
/// prompt02 §4: the invitee's own view. These are deliberately not under /api/circles —
/// the actor is the invited member, not the circle's organizer.
/// </summary>
[ApiController]
[Authorize]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public InvitationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<PendingInvitationDto>>> GetPending() =>
        Ok(await _mediator.Send(new GetMyPendingInvitationsQuery()));

    [HttpPost("{memberId:int}/accept")]
    public async Task<IActionResult> Accept(int memberId)
    {
        await _mediator.Send(new RespondToInvitationCommand(memberId, Accept: true));
        return NoContent();
    }

    [HttpPost("{memberId:int}/decline")]
    public async Task<IActionResult> Decline(int memberId)
    {
        await _mediator.Send(new RespondToInvitationCommand(memberId, Accept: false));
        return NoContent();
    }

    /// <summary>
    /// The signed-in user just opened a WhatsApp invite link (an unregistered-invite token,
    /// see <c>InviteUnregisteredMemberCommand</c>) — link their account to that invite so it
    /// shows up as a normal pending invitation for them.
    /// </summary>
    [HttpPost("link/{token}")]
    public async Task<IActionResult> Link(string token)
    {
        await _mediator.Send(new LinkInvitationTokenCommand(token));
        return NoContent();
    }
}

public record ReviewPaymentClaimRequest(bool Approve, string? RejectionReason);

/// <summary>
/// prompt02 §6: member self-report with evidence, and the organizer's approve/reject.
/// Privacy (claim/evidence/rejection visible only to the submitter and the organizer) is
/// enforced inside the handlers so no endpoint can accidentally leak it.
/// </summary>
[ApiController]
[Authorize]
[Route("api/payment-claims")]
public class PaymentClaimsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentClaimsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Claims I submitted, across all my circles.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<PaymentClaimDto>>> GetMine() =>
        Ok(await _mediator.Send(new GetMyPaymentClaimsQuery()));

    /// <summary>
    /// Multipart so the evidence image/document rides along with the claim. Size is capped both
    /// here (request limit) and in the handler (content type + byte size).
    /// </summary>
    [HttpPost("cycles/{cycleId:int}")]
    [RequestSizeLimit(IEvidenceFileStorage.MaxSizeBytes + 512 * 1024)]
    public async Task<ActionResult<int>> Submit(
        int cycleId,
        [FromForm] decimal claimedAmount,
        [FromForm] string? note,
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

            var id = await _mediator.Send(new SubmitPaymentClaimCommand(cycleId, claimedAmount, note, upload));
            return Ok(id);
        }
        finally
        {
            if (stream is not null) await stream.DisposeAsync();
        }
    }

    [HttpPost("{claimId:int}/review")]
    public async Task<IActionResult> Review(int claimId, ReviewPaymentClaimRequest request)
    {
        await _mediator.Send(new ReviewPaymentClaimCommand(claimId, request.Approve, request.RejectionReason));
        return NoContent();
    }

    /// <summary>The submitting member can still correct amount/note/evidence while Pending.</summary>
    [HttpPut("{claimId:int}")]
    [RequestSizeLimit(IEvidenceFileStorage.MaxSizeBytes + 512 * 1024)]
    public async Task<IActionResult> Update(
        int claimId,
        [FromForm] decimal claimedAmount,
        [FromForm] string? note,
        [FromForm] bool removeEvidence,
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

            await _mediator.Send(new UpdatePaymentClaimCommand(claimId, claimedAmount, note, removeEvidence, upload));
            return NoContent();
        }
        finally
        {
            if (stream is not null) await stream.DisposeAsync();
        }
    }

    /// <summary>The submitting member can withdraw ("unsend") a claim while it's still Pending.</summary>
    [HttpDelete("{claimId:int}")]
    public async Task<IActionResult> Withdraw(int claimId)
    {
        await _mediator.Send(new WithdrawPaymentClaimCommand(claimId));
        return NoContent();
    }

    [HttpGet("{claimId:int}/evidence")]
    public async Task<IActionResult> GetEvidence(int claimId)
    {
        var download = await _mediator.Send(new GetPaymentClaimEvidenceQuery(claimId));
        if (download is null) return NotFound();
        return File(download.Content, download.ContentType, download.FileName);
    }
}

using Dourak.Application.Circles.Commands;
using Dourak.Application.Circles.Dtos;
using Dourak.Application.Circles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dourak.Api.Controllers;

public record CreateCircleRequest(
    string Name, string? Description, string Currency, decimal ContributionAmount,
    DateOnly StartDate);

public record UpdateCircleBasicInfoRequest(string Name, string? Description, DateOnly StartDate, decimal ContributionAmount);
public record SetManualOrderRequest(IReadOnlyList<int> MemberIdsInOrder);
public record ReplaceMemberRequest(int OldMemberId, int NewMemberId);
public record AddUserMemberRequest(string UserId);
public record InviteUnregisteredMemberRequest(string Name);

[ApiController]
[Authorize]
[Route("api/circles")]
public class CirclesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CirclesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CircleSummaryDto>>> GetMyCircles() =>
        Ok(await _mediator.Send(new GetMyCirclesQuery()));

    [HttpGet("{circleId:int}")]
    public async Task<ActionResult<CircleDetailDto>> GetDetail(int circleId) =>
        Ok(await _mediator.Send(new GetCircleDetailQuery(circleId)));

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateCircleRequest request)
    {
        var id = await _mediator.Send(new CreateCircleCommand(
            request.Name, request.Description, request.Currency, request.ContributionAmount,
            request.StartDate));
        return CreatedAtAction(nameof(GetDetail), new { circleId = id }, id);
    }

    /// <summary>prompt03 §1: edit name/description/start date/contribution amount while still a draft.</summary>
    [HttpPut("{circleId:int}/basic-info")]
    public async Task<IActionResult> UpdateBasicInfo(int circleId, UpdateCircleBasicInfoRequest request)
    {
        await _mediator.Send(new UpdateCircleBasicInfoCommand(circleId, request.Name, request.Description, request.StartDate, request.ContributionAmount));
        return NoContent();
    }

    [HttpPost("{circleId:int}/activate")]
    public async Task<IActionResult> Activate(int circleId)
    {
        await _mediator.Send(new ActivateCircleCommand(circleId));
        return NoContent();
    }

    [HttpPost("{circleId:int}/pause")]
    public async Task<IActionResult> Pause(int circleId)
    {
        await _mediator.Send(new PauseCircleCommand(circleId));
        return NoContent();
    }

    [HttpPost("{circleId:int}/resume")]
    public async Task<IActionResult> Resume(int circleId)
    {
        await _mediator.Send(new ResumeCircleCommand(circleId));
        return NoContent();
    }

    [HttpPost("{circleId:int}/cancel")]
    public async Task<IActionResult> Cancel(int circleId)
    {
        await _mediator.Send(new CancelCircleCommand(circleId));
        return NoContent();
    }

    // ----- Members -----

    [HttpGet("{circleId:int}/members")]
    public async Task<ActionResult<IReadOnlyList<MemberDto>>> GetMembers(int circleId) =>
        Ok(await _mediator.Send(new GetMembersQuery(circleId)));

    [HttpPost("{circleId:int}/members")]
    public async Task<ActionResult<int>> AddMember(int circleId, AddMemberCommand body)
    {
        var command = body with { CircleId = circleId };
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetMembers), new { circleId }, id);
    }

    [HttpPut("{circleId:int}/members/{memberId:int}")]
    public async Task<IActionResult> UpdateMember(int circleId, int memberId, UpdateMemberCommand body)
    {
        await _mediator.Send(body with { CircleId = circleId, MemberId = memberId });
        return NoContent();
    }

    [HttpPost("{circleId:int}/members/{memberId:int}/deactivate")]
    public async Task<IActionResult> DeactivateMember(int circleId, int memberId)
    {
        await _mediator.Send(new DeactivateMemberCommand(circleId, memberId));
        return NoContent();
    }

    /// <summary>prompt03 §1: fully remove a member row while the circle is still a draft.</summary>
    [HttpDelete("{circleId:int}/members/{memberId:int}")]
    public async Task<IActionResult> RemoveMember(int circleId, int memberId)
    {
        await _mediator.Send(new RemoveMemberCommand(circleId, memberId));
        return NoContent();
    }

    /// <summary>prompt02 §2: add an already-registered Dourak user (pending their acceptance).</summary>
    [HttpPost("{circleId:int}/members/by-user")]
    public async Task<ActionResult<int>> AddUserMember(int circleId, AddUserMemberRequest request) =>
        Ok(await _mediator.Send(new AddUserMemberCommand(circleId, request.UserId)));

    /// <summary>prompt02 §Draft circles: quick "add me as a member" shortcut for the organizer.</summary>
    [HttpPost("{circleId:int}/members/self")]
    public async Task<ActionResult<int>> AddSelfAsMember(int circleId) =>
        Ok(await _mediator.Send(new AddSelfAsMemberCommand(circleId)));

    /// <summary>Invite someone not yet registered on Dourak, by name, via a WhatsApp link carrying a token.</summary>
    [HttpPost("{circleId:int}/members/invite-unregistered")]
    public async Task<ActionResult<InviteUnregisteredMemberResult>> InviteUnregistered(int circleId, InviteUnregisteredMemberRequest request) =>
        Ok(await _mediator.Send(new InviteUnregisteredMemberCommand(circleId, request.Name)));

    /// <summary>prompt02 §5: send a fresh invitation to a member who declined.</summary>
    [HttpPost("{circleId:int}/members/{memberId:int}/reinvite")]
    public async Task<IActionResult> ReinviteMember(int circleId, int memberId)
    {
        await _mediator.Send(new ReinviteMemberCommand(circleId, memberId));
        return NoContent();
    }

    [HttpPost("{circleId:int}/members/replace")]
    public async Task<IActionResult> ReplaceMember(int circleId, ReplaceMemberRequest request)
    {
        await _mediator.Send(new ReplaceMemberCommand(circleId, request.OldMemberId, request.NewMemberId));
        return NoContent();
    }

    // ----- Payout order -----

    [HttpGet("{circleId:int}/payout-order")]
    public async Task<ActionResult<IReadOnlyList<PayoutOrderEntryDto>>> GetPayoutOrder(int circleId) =>
        Ok(await _mediator.Send(new GetPayoutOrderQuery(circleId)));

    [HttpPut("{circleId:int}/payout-order/manual")]
    public async Task<IActionResult> SetManualOrder(int circleId, SetManualOrderRequest request)
    {
        await _mediator.Send(new SetManualPayoutOrderCommand(circleId, request.MemberIdsInOrder));
        return NoContent();
    }

    /// <summary>Moves one member up (-1) or down (+1) one slot, persisted immediately — no separate Save step.</summary>
    [HttpPost("{circleId:int}/payout-order/{memberId:int}/move")]
    public async Task<IActionResult> MovePayoutPosition(int circleId, int memberId, [FromQuery] int direction)
    {
        await _mediator.Send(new MovePayoutPositionCommand(circleId, memberId, direction));
        return NoContent();
    }

    [HttpPost("{circleId:int}/payout-order/draw")]
    public async Task<IActionResult> RunDraw(int circleId) =>
        Ok(await _mediator.Send(new RunRandomDrawCommand(circleId)));

    [HttpPost("{circleId:int}/payout-order/confirm")]
    public async Task<IActionResult> ConfirmOrder(int circleId)
    {
        await _mediator.Send(new ConfirmPayoutOrderCommand(circleId));
        return NoContent();
    }

    [HttpPost("{circleId:int}/payout-order/reset")]
    public async Task<IActionResult> ResetOrder(int circleId)
    {
        await _mediator.Send(new ResetPayoutOrderCommand(circleId));
        return NoContent();
    }

    // ----- Schedule / dashboard / history -----

    [HttpGet("{circleId:int}/schedule")]
    public async Task<ActionResult<IReadOnlyList<ScheduleCycleDto>>> GetSchedule(int circleId) =>
        Ok(await _mediator.Send(new GetScheduleQuery(circleId)));

    /// <summary>Full per-member payment breakdown for every month, for the Schedule tab.</summary>
    [HttpGet("{circleId:int}/months-detail")]
    public async Task<ActionResult<IReadOnlyList<CircleMonthDto>>> GetMonthsDetail(int circleId) =>
        Ok(await _mediator.Send(new GetCircleMonthsDetailQuery(circleId)));

    [HttpGet("{circleId:int}/dashboard")]
    public async Task<ActionResult<CurrentCycleDashboardDto?>> GetDashboard(int circleId) =>
        Ok(await _mediator.Send(new GetCurrentCycleDashboardQuery(circleId)));

    [HttpGet("{circleId:int}/history")]
    public async Task<ActionResult<IReadOnlyList<CircleHistoryCycleDto>>> GetHistory(int circleId) =>
        Ok(await _mediator.Send(new GetCircleHistoryQuery(circleId)));

    [HttpGet("{circleId:int}/members/{memberId:int}/history")]
    public async Task<ActionResult<MemberHistoryDto>> GetMemberHistory(int circleId, int memberId) =>
        Ok(await _mediator.Send(new GetMemberHistoryQuery(circleId, memberId)));

    // ----- Phase 2 -----

    /// <summary>prompt02 §Draft circles: delete a circle that has no recorded payments yet.</summary>
    [HttpDelete("{circleId:int}")]
    public async Task<IActionResult> Delete(int circleId)
    {
        await _mediator.Send(new DeleteCircleCommand(circleId));
        return NoContent();
    }

    /// <summary>
    /// prompt02 §6: organizer sees every claim in their circle; a member sees only their own
    /// (the filtering lives in the handler, not here).
    /// </summary>
    [HttpGet("{circleId:int}/payment-claims")]
    public async Task<ActionResult<IReadOnlyList<PaymentClaimDto>>> GetPaymentClaims(int circleId, [FromQuery] bool pendingOnly = false) =>
        Ok(await _mediator.Send(new GetCirclePaymentClaimsQuery(circleId, pendingOnly)));
}

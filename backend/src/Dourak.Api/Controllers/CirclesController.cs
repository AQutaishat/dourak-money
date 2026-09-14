using Dourak.Application.Circles.Commands;
using Dourak.Application.Circles.Dtos;
using Dourak.Application.Circles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dourak.Api.Controllers;

public record CreateCircleRequest(
    string Name, string? Description, string Currency, decimal ContributionAmount,
    DateOnly StartDate, bool OrganizerIsMember, string? OrganizerMemberName);

public record SetManualOrderRequest(IReadOnlyList<int> MemberIdsInOrder);
public record ReplaceMemberRequest(int OldMemberId, int NewMemberId);

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
            request.StartDate, request.OrganizerIsMember, request.OrganizerMemberName));
        return CreatedAtAction(nameof(GetDetail), new { circleId = id }, id);
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

    [HttpGet("{circleId:int}/dashboard")]
    public async Task<ActionResult<CurrentCycleDashboardDto?>> GetDashboard(int circleId) =>
        Ok(await _mediator.Send(new GetCurrentCycleDashboardQuery(circleId)));

    [HttpGet("{circleId:int}/history")]
    public async Task<ActionResult<IReadOnlyList<CircleHistoryCycleDto>>> GetHistory(int circleId) =>
        Ok(await _mediator.Send(new GetCircleHistoryQuery(circleId)));

    [HttpGet("{circleId:int}/members/{memberId:int}/history")]
    public async Task<ActionResult<MemberHistoryDto>> GetMemberHistory(int circleId, int memberId) =>
        Ok(await _mediator.Send(new GetMemberHistoryQuery(circleId, memberId)));
}

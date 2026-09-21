using System.ComponentModel;
using System.Text.Json;
using Dourak.Application.Circles.Commands;
using Dourak.Application.Circles.Queries;
using MediatR;
using ModelContextProtocol.Server;

namespace Dourak.Api.Mcp;

/// <summary>
/// Tools exposed to MCP clients (ChatGPT, Claude, etc.) acting on behalf of a signed-in Dourak
/// user — mapped at <c>/mcp</c> in Program.cs, behind the same JWT bearer auth every controller
/// uses. No tool ever takes a user id as a parameter: every one of them just calls an existing
/// MediatR query/command, and those handlers already resolve "who is calling" from
/// <c>ICurrentUserService</c> (backed by the validated JWT's claims), the exact same way a
/// controller action does. An AI assistant can therefore only ever see or act on the data of
/// whichever real Dourak account it authenticated as — never anyone else's, and never by simply
/// asking for a different id.
/// </summary>
[McpServerToolType]
public static class DourakMcpTools
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static string ToJson<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    // ---------- Read tools ----------

    [McpServerTool(Name = "get_my_circles")]
    [Description("Lists every savings circle (جمعية / ROSCA) the authenticated user organizes or is an accepted member of, with status, contribution amount, member count and currency.")]
    public static async Task<string> GetMyCircles(IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetMyCirclesQuery(), cancellationToken));

    [McpServerTool(Name = "get_circle_details")]
    [Description("Gets full details for one savings circle: description, currency, contribution amount, status, member count, and whether the caller organizes it.")]
    public static async Task<string> GetCircleDetails(
        [Description("The circle's numeric id, from get_my_circles.")] int circleId,
        IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetCircleDetailQuery(circleId), cancellationToken));

    [McpServerTool(Name = "get_current_cycle_status")]
    [Description("Gets the current month's collection/payout status for a circle: who has paid, who hasn't (and who is late), amounts collected/expected/outstanding, and who is due to receive this cycle's payout.")]
    public static async Task<string> GetCurrentCycleStatus(
        [Description("The circle's numeric id.")] int circleId,
        IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetCurrentCycleDashboardQuery(circleId), cancellationToken));

    [McpServerTool(Name = "get_circle_members")]
    [Description("Lists a circle's members, their payout order position, and their invitation status (accepted/pending/declined).")]
    public static async Task<string> GetCircleMembers(
        [Description("The circle's numeric id.")] int circleId,
        IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetMembersQuery(circleId), cancellationToken));

    [McpServerTool(Name = "get_circle_history")]
    [Description("Lists a circle's completed past cycles: who received each payout, amounts collected, and who was late or unpaid that cycle.")]
    public static async Task<string> GetCircleHistory(
        [Description("The circle's numeric id.")] int circleId,
        IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetCircleHistoryQuery(circleId), cancellationToken));

    [McpServerTool(Name = "get_pending_invitations")]
    [Description("Lists circle invitations awaiting the authenticated user's accept/decline response.")]
    public static async Task<string> GetPendingInvitations(IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetMyPendingInvitationsQuery(), cancellationToken));

    [McpServerTool(Name = "get_my_payment_claims")]
    [Description("Lists every payment the authenticated user has self-reported across all their circles, and whether each is still pending review, approved, or rejected.")]
    public static async Task<string> GetMyPaymentClaims(IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetMyPaymentClaimsQuery(), cancellationToken));

    // ---------- Write tools ----------

    [McpServerTool(Name = "submit_payment_claim")]
    [Description("Reports that the authenticated user has paid their own contribution for a circle cycle. This does NOT mark it paid immediately — it creates a claim the circle's organizer must approve, exactly like using the 'Report my payment' button in the app.")]
    public static async Task<string> SubmitPaymentClaim(
        [Description("The cycle id to report a payment for — get it from get_current_cycle_status's cycleId field.")] int cycleId,
        [Description("The amount paid, in the circle's own currency.")] decimal amount,
        [Description("Optional note, e.g. how the transfer was made.")] string? note,
        IMediator mediator, CancellationToken cancellationToken)
    {
        var claimId = await mediator.Send(new SubmitPaymentClaimCommand(cycleId, amount, note, null), cancellationToken);
        return ToJson(new { claimId, status = "Pending" });
    }

    [McpServerTool(Name = "withdraw_payment_claim")]
    [Description("Withdraws ('unsends') a payment claim the authenticated user submitted, as long as it hasn't been reviewed by the organizer yet.")]
    public static async Task<string> WithdrawPaymentClaim(
        [Description("The claim id to withdraw — from get_my_payment_claims.")] int claimId,
        IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new WithdrawPaymentClaimCommand(claimId), cancellationToken);
        return ToJson(new { success = true });
    }

    // ---------- Payment reminders ----------

    [McpServerTool(Name = "set_payment_reminder")]
    [Description("Sets up (or updates) a standing email reminder for the authenticated user, sent automatically N days before every future payment due date in a specific circle. Calling this again for the same circle just changes the number of days.")]
    public static async Task<string> SetPaymentReminder(
        [Description("The circle's numeric id.")] int circleId,
        [Description("How many days before the due date to send the reminder (0-30). 0 means on the due date itself.")] int daysBefore,
        IMediator mediator, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new SetPaymentReminderCommand(circleId, daysBefore), cancellationToken);
        return ToJson(new { reminderId = id, circleId, daysBefore });
    }

    [McpServerTool(Name = "remove_payment_reminder")]
    [Description("Turns off the standing payment reminder for a circle.")]
    public static async Task<string> RemovePaymentReminder(
        [Description("The circle's numeric id.")] int circleId,
        IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new RemovePaymentReminderCommand(circleId), cancellationToken);
        return ToJson(new { success = true });
    }

    [McpServerTool(Name = "get_my_payment_reminders")]
    [Description("Lists the authenticated user's standing payment reminders across all circles.")]
    public static async Task<string> GetMyPaymentReminders(IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetMyPaymentRemindersQuery(), cancellationToken));
}

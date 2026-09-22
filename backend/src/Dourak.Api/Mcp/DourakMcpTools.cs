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

    [McpServerTool(Name = "get_my_circles", Title = "List my circles", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Lists every savings circle (جمعية / ROSCA) the authenticated user organizes or is an accepted member of, with status, contribution amount, member count and currency.")]
    public static async Task<string> GetMyCircles(IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetMyCirclesQuery(), cancellationToken));

    [McpServerTool(Name = "get_circle_details", Title = "Get circle details", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Gets full details for one savings circle: description, currency, contribution amount, status, member count, and whether the caller organizes it.")]
    public static async Task<string> GetCircleDetails(
        [Description("The circle's numeric id, from get_my_circles.")] int circleId,
        IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetCircleDetailQuery(circleId), cancellationToken));

    [McpServerTool(Name = "get_current_cycle_status", Title = "Get current cycle status", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Gets the current month's collection/payout status for a circle: who has paid, who hasn't (and who is late), amounts collected/expected/outstanding, and who is due to receive this cycle's payout.")]
    public static async Task<string> GetCurrentCycleStatus(
        [Description("The circle's numeric id.")] int circleId,
        IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetCurrentCycleDashboardQuery(circleId), cancellationToken));

    [McpServerTool(Name = "get_circle_members", Title = "List circle members", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Lists a circle's members, their payout order position, and their invitation status (accepted/pending/declined).")]
    public static async Task<string> GetCircleMembers(
        [Description("The circle's numeric id.")] int circleId,
        IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetMembersQuery(circleId), cancellationToken));

    [McpServerTool(Name = "get_circle_history", Title = "Get circle cycle history", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Lists every one of a circle's monthly cycles (past, current and future) with full per-member detail — the exact same data as the website's \"الدورات الشهرية\" (Monthly Cycles) tab: due date, who received that month's payout and its amount/payment breakdown, and each member's contribution rows (amount, date, and claim status where visible to the caller).")]
    public static async Task<string> GetCircleHistory(
        [Description("The circle's numeric id.")] int circleId,
        IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetCircleMonthsDetailQuery(circleId), cancellationToken));

    [McpServerTool(Name = "get_pending_invitations", Title = "List pending invitations", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Lists circle invitations awaiting the authenticated user's accept/decline response.")]
    public static async Task<string> GetPendingInvitations(IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetMyPendingInvitationsQuery(), cancellationToken));

    [McpServerTool(Name = "get_my_payment_claims", Title = "List my payment claims", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Lists every payment the authenticated user has self-reported across all their circles, and whether each is still pending review, approved, or rejected.")]
    public static async Task<string> GetMyPaymentClaims(IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetMyPaymentClaimsQuery(), cancellationToken));

    // ---------- Write tools ----------

    [McpServerTool(Name = "create_circle", Title = "Create a new circle", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Creates a new savings circle (جمعية) as a Draft owned by the authenticated user, who becomes its organizer. A draft circle has no members yet — use add_circle_member to add them, then activate_circle once at least two members exist and everyone invited has accepted.")]
    public static async Task<string> CreateCircle(
        [Description("Circle name.")] string name,
        [Description("Optional description.")] string? description,
        [Description("3-letter currency code, e.g. JOD, USD.")] string currency,
        [Description("Each member's contribution amount per cycle, in the circle's currency.")] decimal contributionAmount,
        [Description("The first cycle's start date, as an ISO date (YYYY-MM-DD).")] DateOnly startDate,
        IMediator mediator, CancellationToken cancellationToken)
    {
        var circleId = await mediator.Send(new CreateCircleCommand(name, description, currency, contributionAmount, startDate), cancellationToken);
        return ToJson(new { circleId, status = "Draft" });
    }

    [McpServerTool(Name = "add_circle_member", Title = "Add a circle member", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Adds a member to a Draft circle by name (plus optional phone/email/notes) — the same simple \"add by name\" flow the app uses, not a search-and-invite of an existing Dourak user. Only works while the circle is still Draft; the new member participates immediately with the next payout position.")]
    public static async Task<string> AddCircleMember(
        [Description("The circle's numeric id — must currently be Draft.")] int circleId,
        [Description("Member's display name.")] string name,
        [Description("Optional phone number.")] string? phone,
        [Description("Optional email.")] string? email,
        [Description("Optional free-text note.")] string? notes,
        IMediator mediator, CancellationToken cancellationToken)
    {
        var memberId = await mediator.Send(new AddMemberCommand(circleId, name, phone, email, notes), cancellationToken);
        return ToJson(new { memberId });
    }

    [McpServerTool(Name = "activate_circle", Title = "Activate a circle", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Activates a Draft circle, locking in its members and payout order and generating its monthly cycle schedule. Requires at least two participating members and that every invited member has already accepted (get_circle_members shows invitation status) — if no payout order was set manually, activation defaults to the members' current order.")]
    public static async Task<string> ActivateCircle(
        [Description("The circle's numeric id.")] int circleId,
        IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new ActivateCircleCommand(circleId), cancellationToken);
        return ToJson(new { circleId, status = "Active" });
    }

    [McpServerTool(Name = "submit_payment_claim", Title = "Report a payment", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
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

    [McpServerTool(Name = "withdraw_payment_claim", Title = "Withdraw a payment claim", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Withdraws ('unsends') a payment claim the authenticated user submitted, as long as it hasn't been reviewed by the organizer yet.")]
    public static async Task<string> WithdrawPaymentClaim(
        [Description("The claim id to withdraw — from get_my_payment_claims.")] int claimId,
        IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new WithdrawPaymentClaimCommand(claimId), cancellationToken);
        return ToJson(new { success = true });
    }

    // ---------- Payment reminders ----------

    [McpServerTool(Name = "set_payment_reminder", Title = "Set a payment reminder", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Sets up (or updates) a standing email reminder for the authenticated user, sent automatically N days before every future payment due date in a specific circle. Calling this again for the same circle just changes the number of days.")]
    public static async Task<string> SetPaymentReminder(
        [Description("The circle's numeric id.")] int circleId,
        [Description("How many days before the due date to send the reminder (0-30). 0 means on the due date itself.")] int daysBefore,
        IMediator mediator, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new SetPaymentReminderCommand(circleId, daysBefore), cancellationToken);
        return ToJson(new { reminderId = id, circleId, daysBefore });
    }

    [McpServerTool(Name = "remove_payment_reminder", Title = "Remove a payment reminder", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Turns off the standing payment reminder for a circle.")]
    public static async Task<string> RemovePaymentReminder(
        [Description("The circle's numeric id.")] int circleId,
        IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new RemovePaymentReminderCommand(circleId), cancellationToken);
        return ToJson(new { success = true });
    }

    [McpServerTool(Name = "get_my_payment_reminders", Title = "List my payment reminders", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Lists the authenticated user's standing payment reminders across all circles.")]
    public static async Task<string> GetMyPaymentReminders(IMediator mediator, CancellationToken cancellationToken) =>
        ToJson(await mediator.Send(new GetMyPaymentRemindersQuery(), cancellationToken));
}

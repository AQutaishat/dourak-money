using Dourak.Application.Auth;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Enums;
using Dourak.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dourak.Infrastructure.Reminders;

/// <summary>
/// Delivers the "remind me N days before payment" rules a user sets up (e.g. via the MCP server —
/// see <c>SetPaymentReminderCommand</c>). Runs once at startup and then once every 24 hours;
/// for each standing reminder, finds any of that user's circles' still-open cycles whose due date
/// is exactly <c>DaysBefore</c> days away, skips it if that user's own contribution for that
/// cycle is already fully paid or a reminder for that cycle was already sent, and otherwise
/// emails them via <see cref="IEmailSender"/> (a no-op logger until SMTP is actually configured —
/// see <c>DependencyInjection.AddInfrastructure</c>).
///
/// Deliberately simple for a first version: a daily poll rather than precise per-minute
/// scheduling, and email-only (no push/SMS/WhatsApp channel exists yet). Missing the exact due
/// day (e.g. the service was down) just means that one reminder never fires — acceptable for an
/// MVP nudge, not a compliance-grade notification system.
/// </summary>
public class PaymentReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentReminderBackgroundService> _logger;

    public PaymentReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<PaymentReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // A bad run must never crash the host or block every future run.
                _logger.LogError(ex, "Payment reminder sweep failed");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task SendDueRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var appOptions = scope.ServiceProvider.GetRequiredService<IOptions<AppOptions>>().Value;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var reminders = await db.PaymentReminders
            .Include(r => r.Circle)
            .ToListAsync(cancellationToken);

        foreach (var reminder in reminders)
        {
            var targetDueDate = today.AddDays(reminder.DaysBefore);

            var cycle = await db.Cycles
                .Where(c => c.CircleId == reminder.CircleId && c.Status == CycleStatus.Pending && c.DueDate == targetDueDate)
                .Select(c => new { c.Id, c.SequenceNumber, c.DueDate })
                .FirstOrDefaultAsync(cancellationToken);
            if (cycle is null || reminder.HasBeenSentFor(cycle.SequenceNumber)) continue;

            var myContribution = await db.Contributions
                .Where(co => co.CycleId == cycle.Id && co.Member!.UserId == reminder.UserId)
                .Select(co => new { co.ExpectedAmount, co.PaidAmount })
                .FirstOrDefaultAsync(cancellationToken);
            // Not a participating member of this cycle (e.g. left the circle) or already fully paid.
            if (myContribution is null || myContribution.PaidAmount >= myContribution.ExpectedAmount)
            {
                reminder.MarkSentFor(cycle.SequenceNumber);
                continue;
            }

            var profile = await identity.GetProfileAsync(reminder.UserId);
            if (string.IsNullOrWhiteSpace(profile?.Email))
            {
                reminder.MarkSentFor(cycle.SequenceNumber);
                continue;
            }

            var outstanding = myContribution.ExpectedAmount - myContribution.PaidAmount;
            var circleName = reminder.Circle?.Name ?? "—";
            var currency = reminder.Circle?.Currency ?? "";
            var subject = $"تذكير بدفعة {circleName} — Dourak";
            var body = $"""
                <p>مرحباً،</p>
                <p>هذا تذكير بدفعتك القادمة في جمعية "<strong>{circleName}</strong>":</p>
                <ul>
                  <li>المبلغ المتبقي: {outstanding} {currency}</li>
                  <li>تاريخ الاستحقاق: {cycle.DueDate:yyyy-MM-dd}</li>
                </ul>
                <p><a href="{appOptions.FrontendBaseUrl}">افتح تطبيق دورك</a></p>
                """;

            await emailSender.SendAsync(profile.Email!, subject, body, cancellationToken);
            reminder.MarkSentFor(cycle.SequenceNumber);
            _logger.LogInformation("Sent payment reminder to {UserId} for circle {CircleId} cycle {SequenceNumber}",
                reminder.UserId, reminder.CircleId, cycle.SequenceNumber);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

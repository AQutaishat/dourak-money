using Dourak.Domain.Common;

namespace Dourak.Domain.Entities;

/// <summary>
/// One user's standing request to be emailed ahead of a circle's payment due date — e.g. "remind
/// me 2 days before every payment in Family Circle." Created/removed via the MCP server (an AI
/// assistant acting on the user's behalf) or, eventually, the app itself; delivery is handled by
/// <c>PaymentReminderBackgroundService</c> in Infrastructure, which emails the user once per cycle
/// when today falls within <see cref="DaysBefore"/> days of that cycle's due date.
/// </summary>
public class PaymentReminder : AuditableEntity
{
    /// <summary>The user to remind — not necessarily the circle's organizer, any participating member.</summary>
    public string UserId { get; set; } = string.Empty;

    public int CircleId { get; set; }
    public SavingsCircle? Circle { get; set; }

    /// <summary>How many days before a cycle's due date to send the reminder (0 = due-date itself).</summary>
    public int DaysBefore { get; set; }

    /// <summary>
    /// Cycle sequence numbers already reminded, comma-separated (e.g. "1,2,3") — a plain scalar
    /// column rather than a child table, since this is just a small "don't repeat" dedupe set,
    /// not data anyone ever queries independently. Reset never needed: a circle has a bounded,
    /// small number of cycles.
    /// </summary>
    public string SentForSequenceNumbers { get; set; } = string.Empty;

    public bool HasBeenSentFor(int sequenceNumber) =>
        SentForSequenceNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(sequenceNumber.ToString());

    public void MarkSentFor(int sequenceNumber)
    {
        var existing = SentForSequenceNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        existing.Add(sequenceNumber.ToString());
        SentForSequenceNumbers = string.Join(',', existing);
    }
}

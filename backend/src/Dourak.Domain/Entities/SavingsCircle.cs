using Dourak.Domain.Enums;
using Dourak.Domain.Exceptions;
using Dourak.Domain.Common;

namespace Dourak.Domain.Entities;

/// <summary>
/// Aggregate root: a جمعية / Savings Circle. Owns members, payout order and cycles,
/// and is the only place allowed to mutate the payout order or generate the schedule,
/// so the core business rules (BRD §7) stay in one place instead of scattered
/// across application handlers.
/// </summary>
public class SavingsCircle : AuditableEntity
{
    public string OrganizerUserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Currency { get; set; } = "SAR";
    public decimal ContributionAmount { get; set; }
    public CircleFrequency Frequency { get; set; } = CircleFrequency.Monthly;
    public DateOnly StartDate { get; set; }
    public CircleStatus Status { get; set; } = CircleStatus.Draft;
    public PayoutOrderMethod? PayoutOrderMethod { get; set; }

    /// <summary>True once the organizer has confirmed the payout order (manual review or draw).</summary>
    public bool PayoutOrderConfirmed { get; set; }

    public ICollection<CircleMember> Members { get; set; } = new List<CircleMember>();
    public ICollection<PayoutPosition> PayoutPositions { get; set; } = new List<PayoutPosition>();
    public ICollection<Cycle> Cycles { get; set; } = new List<Cycle>();

    private void EnsureDraft(string action)
    {
        if (Status != CircleStatus.Draft)
            throw new DomainException($"Cannot {action}: the circle is no longer in Draft status.");
    }

    /// <summary>
    /// prompt03 §1: while still a draft, the organizer may edit name, description and start
    /// date directly (contribution amount stays non-editable, same as before and after
    /// activation). Once activated these fields are locked, same as everything else the
    /// aggregate freezes on Activate().
    /// </summary>
    public void UpdateBasicInfo(string name, string? description, DateOnly startDate)
    {
        EnsureDraft("edit the circle's basic info");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Circle name is required.");
        Name = name;
        Description = description;
        StartDate = startDate;
    }

    /// <summary>
    /// prompt03 §1: while still a draft, a member row can be removed outright (not just
    /// deactivated) regardless of its invitation status — a draft hasn't started collecting
    /// money yet, so there is no financial history to protect (contrast with
    /// <see cref="CircleMember.Deactivate"/>, which is the only option post-activation).
    /// Also drops any payout position already assigned to this member so the order stays
    /// internally consistent.
    /// </summary>
    public void RemoveMember(int memberId)
    {
        EnsureDraft("remove this member");
        var member = Members.FirstOrDefault(m => m.Id == memberId)
            ?? throw new DomainException("This member does not belong to this circle.");

        var position = PayoutPositions.FirstOrDefault(p => p.MemberId == memberId);
        if (position is not null)
            PayoutPositions.Remove(position);

        Members.Remove(member);
    }

    /// <summary>
    /// Sets a manual payout order. Requires every active member to appear exactly
    /// once (BRD rule #1, #3). Only allowed while the circle is still in Draft.
    /// </summary>
    public void SetManualPayoutOrder(IReadOnlyList<int> memberIdsInOrder)
    {
        EnsureDraft("set the payout order");
        ValidateOrderCoversAllActiveMembers(memberIdsInOrder);

        PayoutPositions.Clear();
        for (var i = 0; i < memberIdsInOrder.Count; i++)
        {
            PayoutPositions.Add(new PayoutPosition
            {
                CircleId = Id,
                MemberId = memberIdsInOrder[i],
                Position = i + 1,
                IsLocked = false
            });
        }
        PayoutOrderMethod = Enums.PayoutOrderMethod.Manual;
        PayoutOrderConfirmed = false;
    }

    /// <summary>
    /// Runs an unbiased random draw (Fisher-Yates) over all active members and
    /// stores the result unconfirmed. Organizer must call ConfirmPayoutOrder to lock it.
    /// </summary>
    public void RunRandomDraw(IRandomShuffler shuffler)
    {
        EnsureDraft("run the random draw");
        var activeMemberIds = Members.Where(m => m.IsParticipating).Select(m => m.Id).ToList();
        if (activeMemberIds.Count == 0)
            throw new DomainException("Cannot run the draw: the circle has no active members.");

        var shuffled = shuffler.Shuffle(activeMemberIds);

        PayoutPositions.Clear();
        for (var i = 0; i < shuffled.Count; i++)
        {
            PayoutPositions.Add(new PayoutPosition
            {
                CircleId = Id,
                MemberId = shuffled[i],
                Position = i + 1,
                IsLocked = false
            });
        }
        PayoutOrderMethod = Enums.PayoutOrderMethod.RandomDraw;
        PayoutOrderConfirmed = false;
    }

    /// <summary>
    /// Confirms the current payout order (manual or drawn). Required before activation.
    /// Does not yet lock positions permanently — that happens on Activate().
    /// </summary>
    public void ConfirmPayoutOrder()
    {
        EnsureDraft("confirm the payout order");
        if (PayoutPositions.Count == 0)
            throw new DomainException("Cannot confirm: no payout order has been set.");
        ValidateOrderCoversAllActiveMembers(PayoutPositions.Select(p => p.MemberId).ToList());
        PayoutOrderConfirmed = true;
    }

    /// <summary>
    /// Explicit, deliberate reset of an unconfirmed or confirmed-but-not-yet-activated
    /// order (BRD §6.9 "require deliberate confirmation"). Not usable after activation —
    /// use ReplaceMemberInPosition for post-activation changes.
    /// </summary>
    public void ResetPayoutOrder()
    {
        EnsureDraft("reset the payout order");
        PayoutPositions.Clear();
        PayoutOrderMethod = null;
        PayoutOrderConfirmed = false;
    }

    private void ValidateOrderCoversAllActiveMembers(IReadOnlyList<int> memberIds)
    {
        var activeMemberIds = Members.Where(m => m.IsParticipating).Select(m => m.Id).ToHashSet();
        var providedIds = memberIds.ToHashSet();

        if (memberIds.Count != memberIds.Distinct().Count())
            throw new DomainException("Each member must appear exactly once in the payout order.");
        if (!providedIds.SetEquals(activeMemberIds))
            throw new DomainException("The payout order must include every active member exactly once.");
    }

    /// <summary>
    /// Activates the circle: locks the payout order and generates the full schedule
    /// (BRD §6.10, §6.11). Member list and contribution amount are frozen from this
    /// point (Phase 1 decision — see Phase 1 Business Decisions).
    /// </summary>
    public void Activate()
    {
        EnsureDraft("activate the circle");

        // prompt02 §Payout Order tab: the separate "Confirm Order" step is gone — activation IS
        // the confirmation (the organizer confirms the member order in the activation dialog),
        // so the order is validated and confirmed here instead of requiring a prior action.
        if (PayoutPositions.Count == 0)
            throw new DomainException("Cannot activate: no payout order has been set.");
        ConfirmPayoutOrder();

        if (ContributionAmount <= 0)
            throw new DomainException("Cannot activate: contribution amount must be greater than zero.");

        foreach (var position in PayoutPositions)
            position.IsLocked = true;

        GenerateSchedule();
        Status = CircleStatus.Active;
    }

    private void GenerateSchedule()
    {
        var activeMemberCount = Members.Count(m => m.IsParticipating);
        var expectedPool = activeMemberCount * ContributionAmount;
        var orderedPositions = PayoutPositions.OrderBy(p => p.Position).ToList();

        Cycles.Clear();
        for (var i = 0; i < orderedPositions.Count; i++)
        {
            var dueDate = AddCycles(StartDate, i);
            var cycle = new Cycle
            {
                CircleId = Id,
                SequenceNumber = i + 1,
                DueDate = dueDate,
                RecipientMemberId = orderedPositions[i].MemberId,
                ExpectedPoolAmount = expectedPool,
                Status = CycleStatus.Pending
            };

            foreach (var member in Members.Where(m => m.IsParticipating))
            {
                cycle.Contributions.Add(new Contribution
                {
                    MemberId = member.Id,
                    ExpectedAmount = ContributionAmount,
                    PaidAmount = 0m
                });
            }

            cycle.Payout = new Payout
            {
                RecipientMemberId = cycle.RecipientMemberId,
                ExpectedAmount = expectedPool,
                Status = PayoutStatus.Pending
            };

            Cycles.Add(cycle);
        }
    }

    private DateOnly AddCycles(DateOnly start, int offset) => Frequency switch
    {
        CircleFrequency.Monthly => start.AddMonths(offset),
        _ => start.AddMonths(offset)
    };

    public void Pause()
    {
        if (Status != CircleStatus.Active)
            throw new DomainException("Only an active circle can be paused.");
        Status = CircleStatus.Paused;
    }

    public void Resume()
    {
        if (Status != CircleStatus.Paused)
            throw new DomainException("Only a paused circle can be resumed.");
        Status = CircleStatus.Active;
    }

    public void Cancel()
    {
        if (Status is CircleStatus.Completed or CircleStatus.Cancelled)
            throw new DomainException("This circle can no longer be cancelled.");
        Status = CircleStatus.Cancelled;
    }

    /// <summary>Marks the circle Completed once every cycle's payout has been paid.</summary>
    public void CompleteIfAllCyclesDone()
    {
        if (Status == CircleStatus.Active && Cycles.All(c => c.Payout != null && c.Payout.Status == PayoutStatus.Paid))
            Status = CircleStatus.Completed;
    }

    /// <summary>
    /// Phase 1 decision: replace a member occupying a not-yet-paid-out future position.
    /// Past/completed cycles keep the original member untouched (history integrity, rule #6).
    /// </summary>
    public void ReplaceMemberInFuturePosition(int oldMemberId, int newMemberId)
    {
        if (Status != CircleStatus.Active)
            throw new DomainException("Members can only be replaced while the circle is active.");

        var position = PayoutPositions.SingleOrDefault(p => p.MemberId == oldMemberId)
            ?? throw new DomainException("The specified member does not hold a payout position in this circle.");

        var futureCycles = Cycles.Where(c => c.RecipientMemberId == oldMemberId && c.Status == CycleStatus.Pending).ToList();
        if (!futureCycles.Any())
            throw new DomainException("This member's payout has already been completed and cannot be reassigned.");

        position.MemberId = newMemberId;
        foreach (var cycle in futureCycles)
            cycle.RecipientMemberId = newMemberId;
    }
}

/// <summary>Abstraction over shuffling so the draw is testable and demonstrably unbiased.</summary>
public interface IRandomShuffler
{
    IReadOnlyList<int> Shuffle(IReadOnlyList<int> items);
}

/// <summary>
/// Unbiased Fisher-Yates shuffle using a cryptographically secure RNG, so the
/// random قرعة is demonstrably fair rather than relying on System.Random's
/// weaker distribution (BRD §6.9: "should feel... fair").
/// </summary>
public sealed class CryptoRandomShuffler : IRandomShuffler
{
    public IReadOnlyList<int> Shuffle(IReadOnlyList<int> items)
    {
        var array = items.ToArray();
        for (var i = array.Length - 1; i > 0; i--)
        {
            var j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
        return array;
    }
}

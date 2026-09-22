using Dourak.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Common.Interfaces;

/// <summary>
/// Persistence seam the Application layer depends on, implemented by the EF Core
/// DbContext in Infrastructure. Keeps Application testable without a real database
/// and avoids adding a redundant repository/unit-of-work layer on top of EF Core,
/// which is already a unit of work.
/// </summary>
public interface IAppDbContext
{
    DbSet<SavingsCircle> Circles { get; }
    DbSet<CircleMember> CircleMembers { get; }
    DbSet<PayoutPosition> PayoutPositions { get; }
    DbSet<Cycle> Cycles { get; }
    DbSet<Contribution> Contributions { get; }
    DbSet<ContributionPayment> ContributionPayments { get; }
    DbSet<Payout> Payouts { get; }
    DbSet<PayoutPayment> PayoutPayments { get; }
    DbSet<PaymentClaim> PaymentClaims { get; }
    DbSet<PaymentReminder> PaymentReminders { get; }
    DbSet<OAuthClient> OAuthClients { get; }
    DbSet<OAuthAuthorizationCode> OAuthAuthorizationCodes { get; }
    DbSet<OAuthRefreshToken> OAuthRefreshTokens { get; }
    DbSet<SupportRequest> SupportRequests { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Needed for the "delete a circle that has no payments yet" rule (prompt02 §Draft circles),
    /// which removes an aggregate root rather than mutating it.
    /// </summary>
    void RemoveCircle(SavingsCircle circle);
}

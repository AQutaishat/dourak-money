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
    DbSet<Payout> Payouts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

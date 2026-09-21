using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using Dourak.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Infrastructure.Persistence;

public class DourakDbContext : IdentityDbContext<ApplicationUser>, IAppDbContext
{
    public DourakDbContext(DbContextOptions<DourakDbContext> options) : base(options) { }

    public DbSet<SavingsCircle> Circles => Set<SavingsCircle>();
    public DbSet<CircleMember> CircleMembers => Set<CircleMember>();
    public DbSet<PayoutPosition> PayoutPositions => Set<PayoutPosition>();
    public DbSet<Cycle> Cycles => Set<Cycle>();
    public DbSet<Contribution> Contributions => Set<Contribution>();
    public DbSet<ContributionPayment> ContributionPayments => Set<ContributionPayment>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<PayoutPayment> PayoutPayments => Set<PayoutPayment>();
    public DbSet<PaymentClaim> PaymentClaims => Set<PaymentClaim>();
    public DbSet<PaymentReminder> PaymentReminders => Set<PaymentReminder>();
    public DbSet<OAuthClient> OAuthClients => Set<OAuthClient>();
    public DbSet<OAuthAuthorizationCode> OAuthAuthorizationCodes => Set<OAuthAuthorizationCode>();
    public DbSet<OAuthRefreshToken> OAuthRefreshTokens => Set<OAuthRefreshToken>();

    public void RemoveCircle(SavingsCircle circle) => Circles.Remove(circle);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(DourakDbContext).Assembly);
    }
}

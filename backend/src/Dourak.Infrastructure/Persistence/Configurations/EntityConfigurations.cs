using Dourak.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dourak.Infrastructure.Persistence.Configurations;

public class SavingsCircleConfiguration : IEntityTypeConfiguration<SavingsCircle>
{
    public void Configure(EntityTypeBuilder<SavingsCircle> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Currency).IsRequired().HasMaxLength(3);
        builder.Property(c => c.ContributionAmount).HasPrecision(18, 2);
        builder.Property(c => c.OrganizerUserId).IsRequired();
        builder.HasIndex(c => c.OrganizerUserId);

        builder.HasMany(c => c.Members).WithOne(m => m.Circle!).HasForeignKey(m => m.CircleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.PayoutPositions).WithOne(p => p.Circle!).HasForeignKey(p => p.CircleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.Cycles).WithOne(cy => cy.Circle!).HasForeignKey(cy => cy.CircleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CircleMemberConfiguration : IEntityTypeConfiguration<CircleMember>
{
    public void Configure(EntityTypeBuilder<CircleMember> builder)
    {
        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Phone).HasMaxLength(50);
        builder.Property(m => m.Email).HasMaxLength(200);
    }
}

public class PayoutPositionConfiguration : IEntityTypeConfiguration<PayoutPosition>
{
    public void Configure(EntityTypeBuilder<PayoutPosition> builder)
    {
        // Business rule: each active member appears exactly once, and each position is unique per circle.
        builder.HasIndex(p => new { p.CircleId, p.Position }).IsUnique();
        builder.HasIndex(p => new { p.CircleId, p.MemberId }).IsUnique();

        builder.HasOne(p => p.Member).WithOne(m => m.PayoutPosition!).HasForeignKey<PayoutPosition>(p => p.MemberId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CycleConfiguration : IEntityTypeConfiguration<Cycle>
{
    public void Configure(EntityTypeBuilder<Cycle> builder)
    {
        builder.Property(c => c.ExpectedPoolAmount).HasPrecision(18, 2);
        builder.HasIndex(c => new { c.CircleId, c.SequenceNumber }).IsUnique();

        builder.HasOne(c => c.Recipient).WithMany().HasForeignKey(c => c.RecipientMemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(c => c.Contributions).WithOne(co => co.Cycle!).HasForeignKey(co => co.CycleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.Payout).WithOne(p => p.Cycle!).HasForeignKey<Payout>(p => p.CycleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContributionConfiguration : IEntityTypeConfiguration<Contribution>
{
    public void Configure(EntityTypeBuilder<Contribution> builder)
    {
        builder.Property(c => c.ExpectedAmount).HasPrecision(18, 2);
        builder.Property(c => c.PaidAmount).HasPrecision(18, 2);
        // Business rule: one contribution record per member per cycle (Phase 1 decision — no advance payments).
        builder.HasIndex(c => new { c.CycleId, c.MemberId }).IsUnique();

        builder.HasOne(c => c.Member).WithMany(m => m.Contributions).HasForeignKey(c => c.MemberId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.Property(p => p.ExpectedAmount).HasPrecision(18, 2);
        builder.Property(p => p.ActualAmount).HasPrecision(18, 2);

        builder.HasOne(p => p.Recipient).WithMany().HasForeignKey(p => p.RecipientMemberId).OnDelete(DeleteBehavior.Restrict);
    }
}

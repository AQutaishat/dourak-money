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

        // Phase 2: a registered user appears at most once per circle. The unique index makes
        // the "already a member" rule impossible to violate even under a race.
        builder.Property(m => m.UserId).HasMaxLength(450);
        builder.Ignore(m => m.IsParticipating);
        builder.HasIndex(m => new { m.CircleId, m.UserId }).IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL");
        builder.HasIndex(m => m.UserId);
    }
}

public class PaymentClaimConfiguration : IEntityTypeConfiguration<PaymentClaim>
{
    public void Configure(EntityTypeBuilder<PaymentClaim> builder)
    {
        builder.Property(pc => pc.ClaimedAmount).HasPrecision(18, 2);
        builder.Property(pc => pc.SubmittedByUserId).IsRequired().HasMaxLength(450);
        builder.Property(pc => pc.ReviewedByUserId).HasMaxLength(450);
        builder.Property(pc => pc.Note).HasMaxLength(1000);
        builder.Property(pc => pc.RejectionReason).HasMaxLength(1000);
        builder.Property(pc => pc.EvidenceStoredFileName).HasMaxLength(200);
        builder.Property(pc => pc.EvidenceOriginalFileName).HasMaxLength(260);
        builder.Property(pc => pc.EvidenceContentType).HasMaxLength(100);
        builder.Ignore(pc => pc.HasEvidence);

        builder.HasIndex(pc => new { pc.CircleId, pc.Status });
        builder.HasIndex(pc => pc.SubmittedByUserId);

        builder.HasOne(pc => pc.Contribution).WithMany().HasForeignKey(pc => pc.ContributionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(pc => pc.Member).WithMany().HasForeignKey(pc => pc.MemberId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PaymentReminderConfiguration : IEntityTypeConfiguration<PaymentReminder>
{
    public void Configure(EntityTypeBuilder<PaymentReminder> builder)
    {
        builder.Property(r => r.UserId).IsRequired().HasMaxLength(450);
        builder.Property(r => r.SentForSequenceNumbers).HasMaxLength(500);

        // One reminder rule per user per circle — asking again just updates DaysBefore.
        builder.HasIndex(r => new { r.UserId, r.CircleId }).IsUnique();

        builder.HasOne(r => r.Circle).WithMany().HasForeignKey(r => r.CircleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OAuthClientConfiguration : IEntityTypeConfiguration<OAuthClient>
{
    public void Configure(EntityTypeBuilder<OAuthClient> builder)
    {
        builder.Property(c => c.ClientId).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ClientName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.RedirectUris).IsRequired().HasMaxLength(2000);
        builder.HasIndex(c => c.ClientId).IsUnique();
    }
}

public class OAuthAuthorizationCodeConfiguration : IEntityTypeConfiguration<OAuthAuthorizationCode>
{
    public void Configure(EntityTypeBuilder<OAuthAuthorizationCode> builder)
    {
        builder.Property(c => c.Code).IsRequired().HasMaxLength(200);
        builder.Property(c => c.ClientId).IsRequired().HasMaxLength(100);
        builder.Property(c => c.UserId).IsRequired().HasMaxLength(450);
        builder.Property(c => c.RedirectUri).IsRequired().HasMaxLength(2000);
        builder.Property(c => c.CodeChallenge).IsRequired().HasMaxLength(200);
        builder.Property(c => c.CodeChallengeMethod).IsRequired().HasMaxLength(20);
        builder.HasIndex(c => c.Code).IsUnique();
    }
}

public class OAuthRefreshTokenConfiguration : IEntityTypeConfiguration<OAuthRefreshToken>
{
    public void Configure(EntityTypeBuilder<OAuthRefreshToken> builder)
    {
        builder.Property(t => t.Token).IsRequired().HasMaxLength(200);
        builder.Property(t => t.ClientId).IsRequired().HasMaxLength(100);
        builder.Property(t => t.UserId).IsRequired().HasMaxLength(450);
        builder.HasIndex(t => t.Token).IsUnique();
    }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<Dourak.Infrastructure.Identity.ApplicationUser>
{
    public void Configure(EntityTypeBuilder<Dourak.Infrastructure.Identity.ApplicationUser> builder)
    {
        builder.Property(u => u.DisplayName).HasMaxLength(200);
        builder.Property(u => u.NormalizedDisplayName).HasMaxLength(200);
        builder.Property(u => u.NormalizedPhoneNumber).HasMaxLength(50);

        // prompt02 §8: name/phone uniqueness enforced in the database on the normalized value,
        // so it holds regardless of which code path writes a profile. Filtered so the many users
        // who leave these blank don't collide on NULL.
        builder.HasIndex(u => u.NormalizedDisplayName).IsUnique()
            .HasFilter("\"NormalizedDisplayName\" IS NOT NULL");
        builder.HasIndex(u => u.NormalizedPhoneNumber).IsUnique()
            .HasFilter("\"NormalizedPhoneNumber\" IS NOT NULL");
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

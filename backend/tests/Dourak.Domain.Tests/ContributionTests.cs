using Dourak.Domain.Entities;
using Dourak.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Dourak.Domain.Tests;

public class ContributionTests
{
    [Fact]
    public void RecordPayment_Partial_SetsPartiallyPaidStatus()
    {
        var contribution = new Contribution { ExpectedAmount = 1000m };

        contribution.RecordPayment(600m, DateTimeOffset.UtcNow, Dourak.Domain.Enums.PaymentMethod.Cash, null, "organizer");

        contribution.PaidAmount.Should().Be(600m);
        contribution.StoredStatus.Should().Be(Dourak.Domain.Enums.ContributionStatus.PartiallyPaid);
    }

    [Fact]
    public void RecordPayment_ExceedingExpected_Throws()
    {
        var contribution = new Contribution { ExpectedAmount = 1000m };

        var act = () => contribution.RecordPayment(1200m, DateTimeOffset.UtcNow, null, null, "organizer");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordPayment_Negative_Throws()
    {
        var contribution = new Contribution { ExpectedAmount = 1000m };

        var act = () => contribution.RecordPayment(-1m, DateTimeOffset.UtcNow, null, null, "organizer");

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0, false, "Unpaid")]
    [InlineData(500, false, "PartiallyPaid")]
    [InlineData(1000, false, "Paid")]
    [InlineData(0, true, "Late")]
    [InlineData(500, true, "Late")]
    [InlineData(1000, true, "Paid")]
    public void ComputeDisplayStatus_DerivesFromDueDateAndBalance(decimal paid, bool overdue, string expected)
    {
        var due = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var now = overdue ? due.AddDays(5) : due.AddDays(-2);
        var contribution = new Contribution { ExpectedAmount = 1000m, PaidAmount = paid };

        contribution.ComputeDisplayStatus(due, now).Should().Be(expected);
    }
}

using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Circles.Queries;

/// <summary>
/// Evidence attached to one payout installment (e.g. a transfer screenshot). Downloadable by the
/// circle's organizer and by the recipient the payout was made to — the same privacy scope used
/// for payment-claim evidence.
/// </summary>
public record GetPayoutPaymentEvidenceQuery(int PayoutPaymentId) : IRequest<EvidenceDownload?>;

public class GetPayoutPaymentEvidenceQueryHandler : IRequestHandler<GetPayoutPaymentEvidenceQuery, EvidenceDownload?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEvidenceFileStorage _storage;

    public GetPayoutPaymentEvidenceQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IEvidenceFileStorage storage)
    {
        _db = db;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<EvidenceDownload?> Handle(GetPayoutPaymentEvidenceQuery request, CancellationToken cancellationToken)
    {
        var payment = await _db.PayoutPayments
            .Include(p => p.Payout).ThenInclude(p => p!.Cycle).ThenInclude(c => c!.Circle)
            .Include(p => p.Payout).ThenInclude(p => p!.Recipient)
            .FirstOrDefaultAsync(p => p.Id == request.PayoutPaymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(PayoutPayment), request.PayoutPaymentId);

        var payout = payment.Payout!;
        var organizerUserId = payout.Cycle!.Circle!.OrganizerUserId;
        var recipientUserId = payout.Recipient!.UserId;

        var mayView = organizerUserId == _currentUser.UserId || (recipientUserId is not null && recipientUserId == _currentUser.UserId);
        if (!mayView)
            throw new ForbiddenAccessException("You do not have access to this payout.");

        if (!payment.HasEvidence) return null;

        var stream = await _storage.OpenAsync(payment.EvidenceStoredFileName!, cancellationToken);
        if (stream is null) return null;

        return new EvidenceDownload(
            stream,
            payment.EvidenceContentType ?? "application/octet-stream",
            payment.EvidenceOriginalFileName ?? payment.EvidenceStoredFileName!);
    }
}

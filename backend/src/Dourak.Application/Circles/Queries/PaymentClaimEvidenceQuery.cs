using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Circles.Queries;

public record EvidenceDownload(Stream Content, string ContentType, string FileName);

/// <summary>
/// prompt02 §6 privacy rule, enforced at the only place the bytes can be reached: evidence is
/// downloadable by the member who submitted the claim and by the circle's organizer — nobody else,
/// including other members of the same circle.
/// </summary>
public record GetPaymentClaimEvidenceQuery(int ClaimId) : IRequest<EvidenceDownload?>;

public class GetPaymentClaimEvidenceQueryHandler : IRequestHandler<GetPaymentClaimEvidenceQuery, EvidenceDownload?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEvidenceFileStorage _storage;

    public GetPaymentClaimEvidenceQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IEvidenceFileStorage storage)
    {
        _db = db;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<EvidenceDownload?> Handle(GetPaymentClaimEvidenceQuery request, CancellationToken cancellationToken)
    {
        var claim = await _db.PaymentClaims.FirstOrDefaultAsync(pc => pc.Id == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentClaim), request.ClaimId);

        var organizerUserId = await _db.Circles.Where(c => c.Id == claim.CircleId)
            .Select(c => c.OrganizerUserId).FirstOrDefaultAsync(cancellationToken);

        var mayView = claim.SubmittedByUserId == _currentUser.UserId || organizerUserId == _currentUser.UserId;
        if (!mayView)
            throw new ForbiddenAccessException("You do not have access to this payment claim.");

        if (!claim.HasEvidence) return null;

        var stream = await _storage.OpenAsync(claim.EvidenceStoredFileName!, cancellationToken);
        if (stream is null) return null;

        return new EvidenceDownload(
            stream,
            claim.EvidenceContentType ?? "application/octet-stream",
            claim.EvidenceOriginalFileName ?? claim.EvidenceStoredFileName!);
    }
}

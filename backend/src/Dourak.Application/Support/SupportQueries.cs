using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Support;

public record SupportRequestDto(
    int Id, string? Name, string Email, string Message, DateTimeOffset CreatedAt,
    bool HasAttachment, string? AttachmentOriginalFileName);

/// <summary>Admin-only — newest first, the whole queue (no pagination yet; volume is low).</summary>
public record GetSupportRequestsQuery : IRequest<IReadOnlyList<SupportRequestDto>>;

public class GetSupportRequestsQueryHandler : IRequestHandler<GetSupportRequestsQuery, IReadOnlyList<SupportRequestDto>>
{
    private readonly IAppDbContext _db;
    public GetSupportRequestsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SupportRequestDto>> Handle(GetSupportRequestsQuery request, CancellationToken cancellationToken) =>
        await _db.SupportRequests
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new SupportRequestDto(r.Id, r.Name, r.Email, r.Message, r.CreatedAt, r.HasAttachment, r.AttachmentOriginalFileName))
            .ToListAsync(cancellationToken);
}

public record SupportAttachmentDownload(Stream Content, string ContentType, string FileName);

public record GetSupportRequestAttachmentQuery(int SupportRequestId) : IRequest<SupportAttachmentDownload?>;

public class GetSupportRequestAttachmentQueryHandler : IRequestHandler<GetSupportRequestAttachmentQuery, SupportAttachmentDownload?>
{
    private readonly IAppDbContext _db;
    private readonly IEvidenceFileStorage _storage;

    public GetSupportRequestAttachmentQueryHandler(IAppDbContext db, IEvidenceFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<SupportAttachmentDownload?> Handle(GetSupportRequestAttachmentQuery request, CancellationToken cancellationToken)
    {
        var supportRequest = await _db.SupportRequests.FirstOrDefaultAsync(r => r.Id == request.SupportRequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(SupportRequest), request.SupportRequestId);

        if (!supportRequest.HasAttachment) return null;

        var stream = await _storage.OpenAsync(supportRequest.AttachmentStoredFileName!, cancellationToken);
        if (stream is null) return null;

        return new SupportAttachmentDownload(
            stream,
            supportRequest.AttachmentContentType ?? "application/octet-stream",
            supportRequest.AttachmentOriginalFileName ?? supportRequest.AttachmentStoredFileName!);
    }
}

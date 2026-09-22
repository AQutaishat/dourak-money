using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Dourak.Application.Support;

/// <summary>Public — no sign-in required. Anyone stuck (including a locked-out user) can reach support.</summary>
public record SubmitSupportRequestCommand(string? Name, string Email, string Message, EvidenceUpload? Attachment) : IRequest<int>;

public class SubmitSupportRequestCommandValidator : AbstractValidator<SubmitSupportRequestCommand>
{
    public SubmitSupportRequestCommandValidator()
    {
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Attachment!.ContentType)
            .Must(ct => IEvidenceFileStorage.AllowedContentTypes.Contains(ct))
            .WithMessage("Attachment must be an image or a PDF.")
            .When(x => x.Attachment is not null);
        RuleFor(x => x.Attachment!.SizeBytes)
            .LessThanOrEqualTo(IEvidenceFileStorage.MaxSizeBytes)
            .WithMessage("Attachment must be 5 MB or smaller.")
            .When(x => x.Attachment is not null);
    }
}

public class SubmitSupportRequestCommandHandler : IRequestHandler<SubmitSupportRequestCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly IEvidenceFileStorage _storage;

    public SubmitSupportRequestCommandHandler(IAppDbContext db, IEvidenceFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<int> Handle(SubmitSupportRequestCommand request, CancellationToken cancellationToken)
    {
        var supportRequest = new SupportRequest
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            Email = request.Email.Trim(),
            Message = request.Message.Trim(),
        };

        if (request.Attachment is not null)
        {
            var stored = await _storage.SaveAsync(request.Attachment, cancellationToken);
            supportRequest.AttachmentStoredFileName = stored.StoredFileName;
            supportRequest.AttachmentOriginalFileName = request.Attachment.FileName;
            supportRequest.AttachmentContentType = stored.ContentType;
        }

        _db.SupportRequests.Add(supportRequest);
        await _db.SaveChangesAsync(cancellationToken);
        return supportRequest.Id;
    }
}

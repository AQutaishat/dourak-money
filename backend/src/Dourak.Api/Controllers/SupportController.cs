using Dourak.Application.Common.Interfaces;
using Dourak.Application.Support;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dourak.Api.Controllers;

/// <summary>
/// Public support inbox — no sign-in required (no [Authorize] here), so a locked-out user can
/// still reach out. Multipart so an optional attachment (a screenshot of the problem) can ride along.
/// </summary>
[ApiController]
[Route("api/support")]
public class SupportController : ControllerBase
{
    private readonly IMediator _mediator;
    public SupportController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [RequestSizeLimit(IEvidenceFileStorage.MaxSizeBytes + 512 * 1024)]
    public async Task<IActionResult> Submit(
        [FromForm] string? name,
        [FromForm] string email,
        [FromForm] string message,
        IFormFile? attachment)
    {
        EvidenceUpload? upload = null;
        Stream? stream = null;
        try
        {
            if (attachment is not null && attachment.Length > 0)
            {
                stream = attachment.OpenReadStream();
                upload = new EvidenceUpload(attachment.FileName, attachment.ContentType ?? "application/octet-stream", attachment.Length, stream);
            }

            var id = await _mediator.Send(new SubmitSupportRequestCommand(name, email, message, upload));
            return Ok(new { id });
        }
        finally
        {
            if (stream is not null) await stream.DisposeAsync();
        }
    }
}

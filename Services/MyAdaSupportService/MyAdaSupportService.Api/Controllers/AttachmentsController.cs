using Microsoft.AspNetCore.Mvc;
using MyAdaSupportService.Application.DTOs.Attcahment;
using MyAdaSupportService.Application.ServicesInterfaces;

namespace MyAdaSupportService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;

    public AttachmentsController(IAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    [HttpPost]
    public async Task<ActionResult<AttachmentDto>> AddAttachment([FromBody] AttachmentCreateDto dto)
    {
        var result = await _attachmentService.AddAttachmentAsync(dto);
        return Ok(result);
    }

    [HttpGet("request/{requestId:int}")]
    public async Task<ActionResult<List<AttachmentDto>>> GetRequestAttachments(int requestId)
    {
        var result = await _attachmentService.GetRequestAttachmentsAsync(requestId);
        return Ok(result);
    }

    [HttpDelete("{attachmentId:int}")]
    public async Task<IActionResult> RemoveAttachment(int attachmentId)
    {
        await _attachmentService.RemoveAttachmentAsync(attachmentId);
        return NoContent();
    }
}

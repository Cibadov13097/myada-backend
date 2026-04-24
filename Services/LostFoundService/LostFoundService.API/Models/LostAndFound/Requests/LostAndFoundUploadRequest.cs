namespace LostFoundService.Api.Models.LostAndFound.Requests;

public sealed class LostAndFoundUploadRequest
{
    public IFormFile? File { get; set; }
    public IFormFile? Photo { get; set; }
    public IFormFile? Image { get; set; }
    public IFormFile? ImageFile { get; set; }
}

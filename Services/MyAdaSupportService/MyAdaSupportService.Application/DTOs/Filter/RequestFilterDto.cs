namespace MyAdaSupportService.Application.DTOs.Filter;

public class RequestFilterDto
{
    public string? Status{ get; set; }     // open | completed | cancelled
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
    public string? Service { get; set; }       // IT | FM
    public string? Priority { get; set; }
    public string? SortBy { get; set; }        // newest | oldest
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? MemberId { get; set; }
    public Guid? StaffId { get; set; }
}

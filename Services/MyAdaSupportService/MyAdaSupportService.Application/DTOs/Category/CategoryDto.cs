using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.Application.DTOs.Category;

public class CategoryDto
{
    public int Id { get; set; }
    public SupportArea Module { get; set; }
    public string Name { get; set; } = string.Empty;
}

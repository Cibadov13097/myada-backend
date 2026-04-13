using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.Application.DTOs.Category;

public class CategoryCreateDto
{
    public SupportArea Module { get; set; }
    public string Name { get; set; } = string.Empty;
}

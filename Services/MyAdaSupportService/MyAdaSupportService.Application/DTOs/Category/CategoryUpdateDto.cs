using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.Application.DTOs.Category;

public class CategoryUpdateDto
{
    public SupportArea Module { get; set; }
    public string Name { get; set; } = string.Empty;
}
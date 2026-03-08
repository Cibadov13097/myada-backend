using System.ComponentModel.DataAnnotations;

namespace LostFoundService.Application.DTOs.Category
{
    public class UpdateCategoryDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}

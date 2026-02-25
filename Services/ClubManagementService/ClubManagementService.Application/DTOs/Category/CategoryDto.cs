namespace ClubManagementService.Application.DTOs.Category;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

public class CreateCategoryDto
{
    public string Name { get; set; } = null!;
}

public class UpdateCategoryDto
{
    public string Name { get; set; } = null!;
}

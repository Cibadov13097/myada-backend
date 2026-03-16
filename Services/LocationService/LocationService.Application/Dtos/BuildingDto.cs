namespace LocationService.Application.DTOs
{
    public class BuildingDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CreateBuildingDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateBuildingDto
    {
        public string Name { get; set; } = string.Empty;
    }
}


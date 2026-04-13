using AutoMapper;
using MyAdaSupportService.Application.Mapping;

namespace MyAdaSupportService.UnitTests.Application;

public class MappingProfileTests
{
    [Fact]
    public void AutoMapper_application_profile_builds_mapper_instance()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        var mapper = cfg.CreateMapper();
        Assert.NotNull(mapper);
    }
}

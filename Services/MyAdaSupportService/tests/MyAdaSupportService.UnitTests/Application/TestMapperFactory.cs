using AutoMapper;
using MyAdaSupportService.Application.Mapping;

namespace MyAdaSupportService.UnitTests.Application;

internal static class TestMapperFactory
{
    private static readonly Lazy<IMapper> Mapper = new(() =>
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        return cfg.CreateMapper();
    });

    public static IMapper Create() => Mapper.Value;
}

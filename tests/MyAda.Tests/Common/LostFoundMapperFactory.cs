using AutoMapper;
using global::LostFoundService.Application.Mappings;

namespace MyAda.Tests.Common;

public static class LostFoundMapperFactory
{
    public static IMapper Create()
    {
        var configuration = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CategoryProfile>();
            cfg.AddProfile<FoundItemProfile>();
            cfg.AddProfile<LostItemProfile>();
        }, null);

        return configuration.CreateMapper();
    }
}

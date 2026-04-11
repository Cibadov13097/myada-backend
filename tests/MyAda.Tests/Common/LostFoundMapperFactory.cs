using AutoMapper;
using global::LostFoundService.Application.Mappings;
using Microsoft.Extensions.Logging.Abstractions;

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
        }, NullLoggerFactory.Instance);

        return configuration.CreateMapper();
    }
}

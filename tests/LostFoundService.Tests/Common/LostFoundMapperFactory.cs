using AutoMapper;
using global::LostFoundService.Application.Mappings;
using Microsoft.Extensions.Logging.Abstractions;

namespace LostFoundService.Tests.Common;

public static class LostFoundMapperFactory
{
    public static IMapper Create()
    {
        var configuration = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CategoryProfile>();
        }, NullLoggerFactory.Instance);

        return configuration.CreateMapper();
    }
}

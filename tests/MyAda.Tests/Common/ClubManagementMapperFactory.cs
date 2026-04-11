using AutoMapper;
using global::ClubManagementService.Application.Mappings;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyAda.Tests.Common;

public static class ClubManagementMapperFactory
{
    public static IMapper Create()
    {
        var configuration = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ClubManagementProfile>();
        }, NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
        return configuration.CreateMapper();
    }
}

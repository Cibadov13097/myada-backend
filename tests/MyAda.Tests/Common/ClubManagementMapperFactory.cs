using AutoMapper;
using global::ClubManagementService.Application.Mappings;

namespace MyAda.Tests.Common;

public static class ClubManagementMapperFactory
{
    public static IMapper Create()
    {
        var configuration = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ClubManagementProfile>();
        }, null);
        configuration.AssertConfigurationIsValid();
        return configuration.CreateMapper();
    }
}

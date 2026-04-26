using AutoMapper;
using global::ClubManagementService.Application.Mappings;

namespace ClubManagementService.Tests.Common;

public static class ClubManagementMapperFactory
{
    public static IMapper Create()
    {
        var configuration = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ClubManagementProfile>();
        });
        configuration.AssertConfigurationIsValid();
        return configuration.CreateMapper();
    }
}

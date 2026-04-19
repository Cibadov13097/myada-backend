namespace CallService.Api.Realtime;

public interface IIceConfigurationProvider
{
    IceConfigurationDto GetForUser(string userId);
}

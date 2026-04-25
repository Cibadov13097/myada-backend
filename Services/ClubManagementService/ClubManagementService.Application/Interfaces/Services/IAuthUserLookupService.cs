namespace ClubManagementService.Application.Interfaces.Services;

public interface IAuthUserLookupService
{
    Task<IReadOnlyDictionary<string, Guid>> GetUserIdsByOrganizationalIdsAsync(
        IEnumerable<string> organizationalIds,
        CancellationToken cancellationToken = default);
}

namespace ClubManagementService.Application.Interfaces.Services;

public interface IAuthUserLookupService
{
    Task<IReadOnlyDictionary<string, Guid>> GetUserIdsByOrganizationalIdsAsync(
        IEnumerable<string> organizationalIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetAllActiveUserIdsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetUserNamesByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
}

using ClubManagementService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace ClubManagementService.Infrastructure.Services;

public sealed class AuthUserLookupService : IAuthUserLookupService
{
    private readonly string _connectionString;

    public AuthUserLookupService(IConfiguration configuration)
    {
        _connectionString = BuildAuthConnectionString(configuration);
    }

    public async Task<IReadOnlyDictionary<string, Guid>> GetUserIdsByOrganizationalIdsAsync(
        IEnumerable<string> organizationalIds,
        CancellationToken cancellationToken = default)
    {
        var ids = organizationalIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ids.Length == 0)
            return new Dictionary<string, Guid>(StringComparer.Ordinal);

        var results = new Dictionary<string, Guid>(StringComparer.Ordinal);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "Id", "OrganizationalId"
            FROM "AspNetUsers"
            WHERE NOT "IsDeleted"
              AND "OrganizationalId" = ANY (@organizationalIds)
            """;
        command.Parameters.AddWithValue(
            "organizationalIds",
            NpgsqlDbType.Array | NpgsqlDbType.Text,
            ids);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var userIdText = reader.GetString(0);
            var organizationalId = reader.GetString(1);
            if (Guid.TryParse(userIdText, out var userId) && userId != Guid.Empty)
                results[organizationalId] = userId;
        }

        return results;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetUserNamesByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds
            .Where(id => id != Guid.Empty)
            .Select(id => id.ToString("D"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ids.Length == 0)
            return new Dictionary<Guid, string>();

        var results = new Dictionary<Guid, string>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "Id", "FirstName", "LastName"
            FROM "AspNetUsers"
            WHERE "Id" = ANY (@userIds)
            """;
        command.Parameters.AddWithValue("userIds", NpgsqlDbType.Array | NpgsqlDbType.Text, ids);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var userIdText = reader.GetString(0);
            var firstName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var lastName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            
            if (Guid.TryParse(userIdText, out var userId))
            {
                var fullName = $"{firstName} {lastName}".Trim();
                results[userId] = fullName;
            }
        }

        return results;
    }

    private static string BuildAuthConnectionString(IConfiguration configuration)
    {
        var authConnectionString = configuration.GetConnectionString("Auth");
        if (!string.IsNullOrWhiteSpace(authConnectionString))
            return authConnectionString;

        var clubConnectionString = configuration.GetConnectionString("Develop");
        if (string.IsNullOrWhiteSpace(clubConnectionString))
            throw new InvalidOperationException("ConnectionStrings:Auth or ConnectionStrings:Develop must be configured.");

        var builder = new NpgsqlConnectionStringBuilder(clubConnectionString)
        {
            Database = "AuthDb"
        };

        return builder.ConnectionString;
    }
}

namespace MyAdaAttendanceService.Application.Query;

/// <summary>
/// Optional list query. <see cref="SortBy"/> is any scalar field on the response DTO (camelCase or PascalCase).
/// <see cref="SortDir"/> is <c>asc</c> (default) or <c>desc</c>.
/// <see cref="Q"/> / <see cref="Search"/> are used by <c>/search</c> endpoints.
/// </summary>
public class ListQuery
{
    public string? SortBy { get; set; }

    public string? SortDir { get; set; }

    /// <summary>Free-text search term (alias of <see cref="Search"/>).</summary>
    public string? Q { get; set; }

    /// <summary>Free-text search term (alias of <see cref="Q"/>).</summary>
    public string? Search { get; set; }

    public string? SearchTerm => string.IsNullOrWhiteSpace(Q) ? Search : Q;
}

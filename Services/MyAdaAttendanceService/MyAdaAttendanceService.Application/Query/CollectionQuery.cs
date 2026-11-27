using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace MyAdaAttendanceService.Application.Query;

public static class CollectionQuery
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Properties = new();

    public static IReadOnlyList<T> Apply<T>(
        IEnumerable<T> source,
        string? sortBy = null,
        string? sortDir = null,
        string? search = null,
        object? filter = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        IEnumerable<T> items = source;

        if (filter is not null)
            items = ApplyFilter(items, filter);

        if (!string.IsNullOrWhiteSpace(search))
            items = ApplySearch(items, search.Trim());

        if (!string.IsNullOrWhiteSpace(sortBy))
            items = ApplySort(items, sortBy.Trim(), sortDir);

        return items as IReadOnlyList<T> ?? items.ToList();
    }

    private static IEnumerable<T> ApplySort<T>(IEnumerable<T> source, string sortBy, string? sortDir)
    {
        var props = GetQueryableProperties(typeof(T));
        var prop = FindProperty(props, sortBy)
            ?? throw new ArgumentException(
                $"Unknown sort field '{sortBy}'. Allowed: {FormatAllowed(props)}.");

        var descending = IsDescending(sortDir);
        return descending
            ? source.OrderByDescending(item => prop.GetValue(item), QueryValueComparer.Instance)
            : source.OrderBy(item => prop.GetValue(item), QueryValueComparer.Instance);
    }

    private static IEnumerable<T> ApplySearch<T>(IEnumerable<T> source, string term)
    {
        var props = GetQueryableProperties(typeof(T));
        return source.Where(item =>
            props.Any(prop => ValueContains(prop.GetValue(item), term)));
    }

    private static IEnumerable<T> ApplyFilter<T>(IEnumerable<T> source, object filter)
    {
        var itemProps = GetQueryableProperties(typeof(T));
        var criteria = new List<(PropertyInfo ItemProp, object FilterValue)>();

        foreach (var filterProp in GetQueryableProperties(filter.GetType()))
        {
            var raw = filterProp.GetValue(filter);
            if (raw is null)
                continue;
            if (raw is string s && string.IsNullOrWhiteSpace(s))
                continue;

            var itemProp = FindProperty(itemProps, filterProp.Name)
                ?? throw new ArgumentException(
                    $"Unknown filter field '{ToCamel(filterProp.Name)}'. Allowed: {FormatAllowed(itemProps)}.");

            criteria.Add((itemProp, raw));
        }

        if (criteria.Count == 0)
            return source;

        return source.Where(item =>
            criteria.All(criterion =>
                Matches(criterion.ItemProp.GetValue(item), criterion.FilterValue, criterion.ItemProp.PropertyType)));
    }

    private static bool IsDescending(string? sortDir)
    {
        if (string.IsNullOrWhiteSpace(sortDir))
            return false;

        if (sortDir.Equals("asc", StringComparison.OrdinalIgnoreCase)
            || sortDir.Equals("ascending", StringComparison.OrdinalIgnoreCase))
            return false;

        if (sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            || sortDir.Equals("descending", StringComparison.OrdinalIgnoreCase))
            return true;

        throw new ArgumentException("sortDir must be 'asc' or 'desc'.");
    }

    private static PropertyInfo[] GetQueryableProperties(Type type) =>
        Properties.GetOrAdd(type, static t => t
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && IsScalar(p.PropertyType))
            .ToArray());

    private static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(Guid)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly);
    }

    private static PropertyInfo? FindProperty(IEnumerable<PropertyInfo> properties, string name) =>
        properties.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static string FormatAllowed(IEnumerable<PropertyInfo> properties) =>
        string.Join(", ", properties.Select(p => ToCamel(p.Name)));

    private static string ToCamel(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];

    private static bool ValueContains(object? value, string term)
    {
        if (value is null)
            return false;

        foreach (var text in Stringify(value))
        {
            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static IEnumerable<string> Stringify(object value)
    {
        switch (value)
        {
            case string s:
                yield return s;
                yield break;
            case DateTime dt:
                yield return dt.ToString("O", CultureInfo.InvariantCulture);
                yield return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                yield break;
            case DateTimeOffset dto:
                yield return dto.ToString("O", CultureInfo.InvariantCulture);
                yield return dto.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                yield break;
            case DateOnly date:
                yield return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                yield break;
            case TimeOnly time:
                yield return time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                yield return time.ToString("HH:mm", CultureInfo.InvariantCulture);
                yield break;
            case Guid guid:
                yield return guid.ToString("D");
                yield return guid.ToString("N");
                yield break;
            case bool flag:
                yield return flag ? "true" : "false";
                yield break;
            case Enum:
                yield return value.ToString() ?? string.Empty;
                yield return Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                yield break;
            default:
                var text = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(text))
                    yield return text;
                yield break;
        }
    }

    private static bool Matches(object? itemValue, object filterValue, Type itemType)
    {
        if (itemValue is null)
            return false;

        var target = Nullable.GetUnderlyingType(itemType) ?? itemType;

        if (target == typeof(string) || itemValue is string)
        {
            var haystack = Convert.ToString(itemValue, CultureInfo.InvariantCulture) ?? string.Empty;
            var needle = Convert.ToString(filterValue, CultureInfo.InvariantCulture) ?? string.Empty;
            return haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
        }

        if (target == typeof(DateTime) || itemValue is DateTime)
        {
            var itemDt = ToDateTime(itemValue);
            var filterDt = ToDateTime(filterValue);
            if (itemDt is null || filterDt is null)
                return false;
            if (filterDt.Value.TimeOfDay == TimeSpan.Zero)
                return itemDt.Value.Date == filterDt.Value.Date;
            return itemDt.Value == filterDt.Value;
        }

        if (target == typeof(DateTimeOffset) || itemValue is DateTimeOffset)
        {
            var itemDto = ToDateTimeOffset(itemValue);
            var filterDto = ToDateTimeOffset(filterValue);
            if (itemDto is null || filterDto is null)
                return false;
            if (filterDto.Value.TimeOfDay == TimeSpan.Zero)
                return itemDto.Value.Date == filterDto.Value.Date;
            return itemDto.Value == filterDto.Value;
        }

        if (target == typeof(DateOnly) || itemValue is DateOnly)
        {
            var itemDate = ToDateOnly(itemValue);
            var filterDate = ToDateOnly(filterValue);
            return itemDate.HasValue && filterDate.HasValue && itemDate.Value == filterDate.Value;
        }

        if (target == typeof(TimeOnly) || itemValue is TimeOnly)
        {
            var itemTime = ToTimeOnly(itemValue);
            var filterTime = ToTimeOnly(filterValue);
            return itemTime.HasValue && filterTime.HasValue && itemTime.Value == filterTime.Value;
        }

        if (target == typeof(Guid) || itemValue is Guid)
            return TryToGuid(itemValue, out var left) && TryToGuid(filterValue, out var right) && left == right;

        if (target.IsEnum)
        {
            return Enum.TryParse(target, Convert.ToString(filterValue, CultureInfo.InvariantCulture), ignoreCase: true, out var parsed)
                && Equals(itemValue, parsed);
        }

        try
        {
            var converted = Convert.ChangeType(filterValue, target, CultureInfo.InvariantCulture);
            return Equals(itemValue, converted);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            return string.Equals(
                Convert.ToString(itemValue, CultureInfo.InvariantCulture),
                Convert.ToString(filterValue, CultureInfo.InvariantCulture),
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private static DateTime? ToDateTime(object value) => value switch
    {
        DateTime dt => dt,
        DateTimeOffset dto => dto.UtcDateTime,
        DateOnly date => date.ToDateTime(TimeOnly.MinValue),
        _ => DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null
    };

    private static DateTimeOffset? ToDateTimeOffset(object value) => value switch
    {
        DateTimeOffset dto => dto,
        DateTime dt => new DateTimeOffset(dt),
        _ => DateTimeOffset.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null
    };

    private static DateOnly? ToDateOnly(object value) => value switch
    {
        DateOnly date => date,
        DateTime dt => DateOnly.FromDateTime(dt),
        DateTimeOffset dto => DateOnly.FromDateTime(dto.UtcDateTime),
        _ => DateOnly.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null
    };

    private static TimeOnly? ToTimeOnly(object value) => value switch
    {
        TimeOnly time => time,
        DateTime dt => TimeOnly.FromDateTime(dt),
        _ => TimeOnly.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null
    };

    private static bool TryToGuid(object value, out Guid guid)
    {
        if (value is Guid g)
        {
            guid = g;
            return true;
        }

        return Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out guid);
    }

    private sealed class QueryValueComparer : IComparer<object?>
    {
        public static readonly QueryValueComparer Instance = new();

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;
            if (x is string left && y is string right)
                return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
            if (x is IComparable comparable && x.GetType() == y.GetType())
                return comparable.CompareTo(y);
            return string.Compare(
                Convert.ToString(x, CultureInfo.InvariantCulture),
                Convert.ToString(y, CultureInfo.InvariantCulture),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}

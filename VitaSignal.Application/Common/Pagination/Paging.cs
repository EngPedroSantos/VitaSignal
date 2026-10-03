using VitaSignal.Application.Common.Exceptions;

namespace VitaSignal.Application.Common.Pagination;

public static class Paging
{
    public const int DefaultLimit = 100;
    public const int MaxLimit = 500;

    public static int ResolveLimit(int? limit)
    {
        var resolved = limit ?? DefaultLimit;

        if (resolved is < 1 or > MaxLimit)
            throw new InvalidRequestException($"Limit must be between 1 and {MaxLimit}.");

        return resolved;
    }

    public static Page<T> ToPage<T>(IReadOnlyList<T> rows, int limit, Func<T, KeysetCursor> cursorOf)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(cursorOf);

        if (rows.Count <= limit)
            return new Page<T>(rows, null);

        var items = rows.Take(limit).ToList();
        return new Page<T>(items, cursorOf(items[^1]).Encode());
    }
}

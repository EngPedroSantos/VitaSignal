namespace VitaSignal.Application.Common.Pagination;

public sealed record Page<T>(IReadOnlyList<T> Items, string? NextCursor);

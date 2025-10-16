namespace FlowSynx.Application.Extensions;

public static class PaginationExtensions
{
    public static List<T> ToPaginatedList<T>(
        this IEnumerable<T> source,
        int page,
        int pageSize,
        out int totalCount,
        out int normalizedPage,
        out int normalizedPageSize)
    {
        var list = source?.ToList() ?? new List<T>();
        totalCount = list.Count;

        normalizedPage = page < 1 ? 1 : page;
        normalizedPageSize = pageSize < 1
            ? totalCount == 0 ? 1 : totalCount
            : pageSize;

        var skip = (normalizedPage - 1) * normalizedPageSize;
        if (skip >= totalCount)
        {
            normalizedPage = totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);
            skip = (normalizedPage - 1) * normalizedPageSize;
        }

        return list.Skip(skip).Take(normalizedPageSize).ToList();
    }
}

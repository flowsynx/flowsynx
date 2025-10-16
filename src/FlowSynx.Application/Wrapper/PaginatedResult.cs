namespace FlowSynx.Application.Wrapper;

public class PaginatedResult<T> : Result<List<T>>
{
    private PaginatedResult()
    {
    }

    private PaginatedResult(
        bool succeeded,
        List<T>? data,
        List<string>? messages,
        int totalCount,
        int page,
        int pageSize)
    {
        Succeeded = succeeded;
        Messages = messages ?? new List<string>();
        Data = data ?? new List<T>();
        TotalCount = Math.Max(totalCount, 0);
        PageSize = NormalizePageSize(pageSize, TotalCount);
        CurrentPage = NormalizePage(page);
        TotalPages = PageSize == 0
            ? 0
            : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public int TotalCount { get; private set; }

    public int PageSize { get; private set; }

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;

    public static PaginatedResult<T> Success(
        List<T> data,
        int totalCount,
        int page,
        int pageSize)
    {
        return new PaginatedResult<T>(true, data, null, totalCount, page, pageSize);
    }

    public static PaginatedResult<T> Success(List<T> data)
    {
        var list = data ?? new List<T>();
        return new PaginatedResult<T>(true, list, null, list.Count, 1, list.Count == 0 ? 1 : list.Count);
    }

    public static PaginatedResult<T> Failure()
    {
        return new PaginatedResult<T>(false, default, null, 0, 1, 1);
    }

    public static PaginatedResult<T> Failure(string message)
    {
        return new PaginatedResult<T>(false, default, new List<string> { message }, 0, 1, 1);
    }

    public static PaginatedResult<T> Failure(List<string> messages)
    {
        return new PaginatedResult<T>(false, default, messages, 0, 1, 1);
    }

    public static Task<PaginatedResult<T>> SuccessAsync(
        List<T> data,
        int totalCount,
        int page,
        int pageSize)
    {
        return Task.FromResult(Success(data, totalCount, page, pageSize));
    }

    public static Task<PaginatedResult<T>> SuccessAsync(List<T> data)
    {
        return Task.FromResult(Success(data));
    }

    public static Task<PaginatedResult<T>> FailureAsync()
    {
        return Task.FromResult(Failure());
    }

    public static Task<PaginatedResult<T>> FailureAsync(string message)
    {
        return Task.FromResult(Failure(message));
    }

    public static Task<PaginatedResult<T>> FailureAsync(List<string> messages)
    {
        return Task.FromResult(Failure(messages));
    }

    private static int NormalizePage(int page)
    {
        return page < 1 ? 1 : page;
    }

    private static int NormalizePageSize(int pageSize, int totalCount)
    {
        if (pageSize < 1)
        {
            return totalCount == 0 ? 1 : totalCount;
        }

        return pageSize;
    }
}

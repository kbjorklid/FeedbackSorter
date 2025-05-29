namespace FeedbackSorter.SharedKernel;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }

    public PagedResult(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        PageNumber = pageNumber > 0 ? pageNumber : throw new ArgumentOutOfRangeException(nameof(pageNumber));
        PageSize = pageSize > 0 ? pageSize : throw new ArgumentOutOfRangeException(nameof(pageSize));
        TotalCount = totalCount >= 0 ? totalCount : throw new ArgumentOutOfRangeException(nameof(totalCount));
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}

namespace ApiControlePerifericos.Pagination
{
    public class PagedList<T> : List<T> where T : class
    {
        public int CurrentPage { get; }
        public int TotalPages { get; }
        public int PageSize { get; }
        public int TotalCount { get; }

        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;

        public PagedList(List<T> items, int totalCount, int currentPage, int pageSize)
        {
            AddRange(items);
            TotalCount = totalCount;
            CurrentPage = currentPage;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        }

        public static PagedList<T> ToPagedList(IQueryable<T> query, int currentPage, int pageSize)
        {
            var totalCount = query.Count();
            var items = query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return new PagedList<T>(items, totalCount, currentPage, pageSize);
        }
    }
}

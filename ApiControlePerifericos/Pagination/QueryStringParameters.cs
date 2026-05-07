namespace ApiControlePerifericos.Pagination
{
    public abstract class QueryStringParameters
    {
        private const int MaxPageSize = 50;

        public int PageNumber { get; set; } = 1;

        private int _pageSize = MaxPageSize;
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = Math.Clamp(value, 1, MaxPageSize); }
        }
    }
}

namespace ApiControlePerifericos.Pagination
{
    public abstract class QueryStringParameters
    {
        private const int MaxPageSize = 50;

        private int _pageNumber = 1;
        public int PageNumber
        {
            get { return _pageNumber; }
            set { _pageNumber = Math.Max(value, 1); }
        }

        private int _pageSize = MaxPageSize;
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = Math.Clamp(value, 1, MaxPageSize); }
        }
    }
}

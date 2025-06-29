namespace JwtProject.Models
{
    public class PaginationFilter
    {
        private const int MaxPageSize = 50;
        public int PageNumber { get; set; } = 1;
        private int _pageSize = 10;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        public string? SortBy { get; set; }
        public string? Search { get; set; }
    }
    
    public class WorkItemFilter : PaginationFilter
    {
        public string? Status { get; set; }
        public Guid? CreatedById { get; set; }
    }
}
namespace CourseLibrary.API.ResourceParameters;

public class AuthorResourceParameters
{
    private const int maxPageSize = 25;
    public string? MainCategory { get; set; }
    public string? SearchQuery { get; set; }

    public int PageNumber { get; set; } = 1;
    private readonly int _pageSize = 10;

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = (value > maxPageSize) ? maxPageSize : value;
    }

    public string OrderBy { get; set; } = "Name";
}
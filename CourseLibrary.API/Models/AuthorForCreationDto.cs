using static System.String;

namespace CourseLibrary.API.Models;

public class AuthorForCreationDto
{
    public string FirstName { get; set; } = Empty;
    public string LastName { get; set; } = Empty;
    public DateTimeOffset DateOfBirth { get; set; }
    public string MainCategory  { get; set; } = Empty;
    public ICollection<CourseForCreationDto> Courses { get; set; } = new List<CourseForCreationDto>();
}
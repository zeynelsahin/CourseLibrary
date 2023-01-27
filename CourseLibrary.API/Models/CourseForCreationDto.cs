using System.ComponentModel.DataAnnotations;

namespace CourseLibrary.API.Models;

public class CourseForCreationDto
{

    [Required(ErrorMessage = "You should fill out a tittle")]
    [MaxLength(50, ErrorMessage = "The tittle shouldn't have more than 50 characters.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1200, ErrorMessage = "The tittle shouldn't have more than 1200 characters.")]
    public string Description { get; set; } = string.Empty;
}
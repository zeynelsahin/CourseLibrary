using System.ComponentModel.DataAnnotations;
using CourseLibrary.API.ValidationAttributes;

namespace CourseLibrary.API.Models;

[CourseTitleMustBeDifferentFromDescription]
public class CourseForManipulationDto//:IValidatableObject
{
    [Required(ErrorMessage = "You should fill out a tittle")]
    [MaxLength(50, ErrorMessage = "The tittle shouldn't have more than 50 characters.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1200, ErrorMessage = "The tittle shouldn't have more than 1200 characters.")]
    public virtual string Description { get; set; } = string.Empty;

    // public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    // {
    //     if (Title== Description)
    //     {
    //         yield return new ValidationResult("The provided description should be different from the tittle",new []{"Course"});
    //     }
    // }
}
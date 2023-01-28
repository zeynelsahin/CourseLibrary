using System.ComponentModel.DataAnnotations;
using CourseLibrary.API.Models;

namespace CourseLibrary.API.ValidationAttributes;

public class CourseTitleMustBeDifferentFromDescriptionAttribute:ValidationAttribute
{
    public CourseTitleMustBeDifferentFromDescriptionAttribute()
    {
        
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is not CourseForManipulationDto course)
        {
            throw new Exception("Attribute " + $"{nameof(CourseTitleMustBeDifferentFromDescriptionAttribute)}" + $"must be applied to a " + $"{nameof(CourseForManipulationDto)} or derived type.");
        }

        return course.Title==course.Description ? new ValidationResult("The provided description should be different from the title.",new []{nameof(CourseForManipulationDto)}) : ValidationResult.Success;
    }
}
namespace CourseLibrary.API.Services;

public class PropertyMappingValue
{
    public IEnumerable<string> DestinationProperty { get; private set; }
    public bool Revert { get; private set; }

    public PropertyMappingValue(IEnumerable<string> destinationProperties, bool revert= false)
    {
        DestinationProperty = destinationProperties?? throw new ArgumentNullException(nameof(destinationProperties));
        Revert = revert;
    }
}
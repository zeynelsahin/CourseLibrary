﻿using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;

namespace CourseLibrary.API.Services;

public class PropertyMappingService : IPropertyMappingService
{
    private readonly Dictionary<string, PropertyMappingValue> _authorPropertyMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Id", new PropertyMappingValue(new[] { "Id" }) },
        { "MainCategory", new PropertyMappingValue(new[] { "MainCategory" }) },
        { "Age", new PropertyMappingValue(new[] { "DateOfBirth" }) },
        { "Name", new PropertyMappingValue(new[] { "FirstName", "LastName" }) }
    };

    private readonly IList<IPropertyMapping> _propertyMappings= new List<IPropertyMapping>();

    public PropertyMappingService()
    {
        _propertyMappings.Add(new PropertyMapping<AuthorDto, Author>(_authorPropertyMapping));
    }

    public Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>()
    {
        var matchingMapping = _propertyMappings.OfType<PropertyMapping<TSource, TDestination>>();

        if (matchingMapping.Count()==1)
        {
            return matchingMapping.First().MappingDictionary;
        }
        
        throw new Exception($"Cannot find exact property mapping instance for <{typeof(TSource)},{typeof(TDestination)}>");
    }

    public bool ValidMappingExistsFor<TSource, TDestination>(string fields)
    {
        var propertyMapping = GetPropertyMapping<TSource, TDestination>();

        if (string.IsNullOrWhiteSpace(fields))
        {
            return true;
        }

        var fieldAfterSplit = fields.Split(',');

        foreach (var field in fieldAfterSplit)
        {
            var trimmedField = field.Trim();

            var indexOfFirstSpace = trimmedField.IndexOf(" ");
            var propertyName = indexOfFirstSpace == -1 ? trimmedField: trimmedField.Remove(indexOfFirstSpace);

            if (!propertyMapping.ContainsKey(propertyName))
            {
                return false;
            }
        }

        return true;
    }
    
    
}
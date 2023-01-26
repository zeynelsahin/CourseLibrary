﻿using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseLibrary.API.Controllers;


[ApiController]
[Route("api/authorcollections")]
public class AuthorCollectionsController: ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;

    public AuthorCollectionsController(ICourseLibraryRepository courseLibraryRepository, IMapper mapper)
    {
        _courseLibraryRepository = courseLibraryRepository;
        _mapper = mapper;
    }

    [HttpGet("({authorIds})",Name = "GetAuthorCollection")]
    public async Task<ActionResult<IEnumerable<AuthorForCreationDto>>> GetAuthorCollection([ModelBinder(BinderType = typeof(ArrayModelBinder))][FromRoute] IEnumerable<Guid> authorIds)
    {
        var authorEntities = await _courseLibraryRepository.GetAuthorsAsync(authorIds);

        if (authorIds.Count()!=authorEntities.Count())
        {
            return NotFound(); 
        }

        var authorToReturn = _mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
        return Ok(authorToReturn);
    }
    [HttpPost]
    public async Task<ActionResult<IEnumerable<AuthorDto>>> CreateAuthorCollection(IEnumerable<AuthorForCreationDto> authorCollection)
    {
        var authorEntities = _mapper.Map<IEnumerable<Author>>(authorCollection);
        foreach (var author in authorEntities)
        {
            _courseLibraryRepository.AddAuthor(author);
        }

        await _courseLibraryRepository.SaveAsync();

        var authorCollectionToReturn = _mapper.Map<IEnumerable<AuthorDto>>(authorEntities);

        var authorIdsAsString = string.Join(",", authorCollectionToReturn.Select(a => a.Id));
        return CreatedAtRoute("GetAuthorCollection", new { authorIds = authorIdsAsString }, authorCollectionToReturn);  

    }
}

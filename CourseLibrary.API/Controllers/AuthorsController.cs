using System.Text.Json;
using AutoMapper;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;
    private readonly IPropertyMappingService _propertyMappingService;
    public AuthorsController(
        ICourseLibraryRepository courseLibraryRepository,
        IMapper mapper, IPropertyMappingService propertyMappingService)
    {
        _courseLibraryRepository = courseLibraryRepository ??
                                   throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ??
                  throw new ArgumentNullException(nameof(mapper));
        _propertyMappingService = propertyMappingService?? throw new ArgumentNullException(nameof(propertyMappingService));
    }

    [HttpGet(Name = "GetAuthors")]
    public async Task<ActionResult<IEnumerable<AuthorDto>>> GetAuthors([FromQuery] AuthorResourceParameters resourceParameters)
    {

        if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Entities.Author>(resourceParameters.OrderBy))
        {
            return BadRequest();
        }
        var authorsFromRepo = await _courseLibraryRepository
            .GetAuthorsAsync(resourceParameters);
        var previousPageLink = authorsFromRepo.HasPrevious ? CreateAuthorsResourceUri(resourceParameters, ResourceUriType.PreviousPage) : null;
        var nextPageLink = authorsFromRepo.HasNext ? CreateAuthorsResourceUri(resourceParameters, ResourceUriType.NextPage) : null;

        var paginationMetadata = new
        {
            totalCount = authorsFromRepo.TotalCount,
            pageSize = authorsFromRepo.PageSize,
            currentPage = authorsFromRepo.CurrentPage,
            totalPages = authorsFromRepo.TotalPages,
            previousPageLink,
            nextPageLink
        };
        Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
        return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo));
    }

    private string? CreateAuthorsResourceUri(AuthorResourceParameters resourceParameters, ResourceUriType type)
    {
        switch (type)
        {
            case ResourceUriType.NextPage:
                return Url.Link("GetAuthors", new
                {
                    orderBy = resourceParameters.OrderBy,
                    pageNumber = resourceParameters.PageNumber + 1,
                    pageSize = resourceParameters.PageSize,
                    mainCategory = resourceParameters.MainCategory,
                    searchQuery = resourceParameters.SearchQuery
                });
                break;
            case ResourceUriType.PreviousPage:
                return Url.Link("GetAuthors", new
                {
                    orderBy = resourceParameters.OrderBy,
                    pageNumber = resourceParameters.PageNumber - 1,
                    pageSize = resourceParameters.PageSize,
                    mainCategory = resourceParameters.MainCategory,
                    searchQuery = resourceParameters.SearchQuery
                });
            default:
                return Url.Link("GetAuthors", new
                {
                    orderBy = resourceParameters.OrderBy,
                    pageNumber = resourceParameters.PageNumber,
                    pageSize = resourceParameters.PageSize,
                    mainCategory = resourceParameters.MainCategory,
                    searchQuery = resourceParameters.SearchQuery
                });
        }
    }

    [HttpGet("{authorId}", Name = "GetAuthor")]
    public async Task<ActionResult<AuthorDto>> GetAuthor(Guid authorId)
    {
        // get author from repo
        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        // return author
        return Ok(_mapper.Map<AuthorDto>(authorFromRepo));
    }

    [HttpPost]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        return CreatedAtRoute("GetAuthor",
            new { authorId = authorToReturn.Id },
            authorToReturn);
    }

    [HttpOptions]
    public IActionResult GetAuthorsOptions()
    {
        Response.Headers.Add("Allow", "GET,HEAD,POST,OPTIONS");
        return Ok();
    }
}
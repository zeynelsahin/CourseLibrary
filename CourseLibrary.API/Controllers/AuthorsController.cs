using System.Dynamic;
using System.Text.Json;
using AutoMapper;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;
    private readonly IPropertyMappingService _propertyMappingService;
    private readonly IPropertyCheckerService _propertyCheckerService;
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    public AuthorsController(
        ICourseLibraryRepository courseLibraryRepository,
        IMapper mapper, IPropertyMappingService propertyMappingService)
    {
        _courseLibraryRepository = courseLibraryRepository ??
                                   throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ??
                  throw new ArgumentNullException(nameof(mapper));
        _propertyMappingService = propertyMappingService ?? throw new ArgumentNullException(nameof(propertyMappingService));
    }

    [HttpGet(Name = "GetAuthors")]
    public async Task<IActionResult> GetAuthors([FromQuery] AuthorResourceParameters resourceParameters)
    {
        if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Entities.Author>(resourceParameters.OrderBy))
        {
            return BadRequest();
        }

        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(resourceParameters.Fields))
        {
            return BadRequest(_problemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: 400, detail: $"Not all requested data shaping fields exist on the resource: {resourceParameters.Fields}"));
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
        return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo).ShepData(resourceParameters.Fields));
    }

    private string? CreateAuthorsResourceUri(AuthorResourceParameters resourceParameters, ResourceUriType type)
    {
        return type switch
        {
            ResourceUriType.NextPage => Url.Link("GetAuthors", new
            {
                fields = resourceParameters.Fields,
                orderBy = resourceParameters.OrderBy,
                pageNumber = resourceParameters.PageNumber + 1,
                pageSize = resourceParameters.PageSize,
                mainCategory = resourceParameters.MainCategory,
                searchQuery = resourceParameters.SearchQuery
            }),
            ResourceUriType.PreviousPage => Url.Link("GetAuthors", new
            {
                fields = resourceParameters.Fields,
                orderBy = resourceParameters.OrderBy,
                pageNumber = resourceParameters.PageNumber - 1,
                pageSize = resourceParameters.PageSize,
                mainCategory = resourceParameters.MainCategory,
                searchQuery = resourceParameters.SearchQuery
            }),
            _ => Url.Link("GetAuthors", new
            {
                fields = resourceParameters.Fields,
                orderBy = resourceParameters.OrderBy,
                pageNumber = resourceParameters.PageNumber,
                pageSize = resourceParameters.PageSize,
                mainCategory = resourceParameters.MainCategory,
                searchQuery = resourceParameters.SearchQuery
            })
        };
    }

    [HttpGet("{authorId:guid}", Name = "GetAuthor")]
    public async Task<IActionResult> GetAuthor(Guid authorId, string? fields)
    {
        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
        {
            return BadRequest(_problemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: 400, detail: $"Not all requested data shaping fields exist on the resource: {fields}"));
        }
        // get author from repo
        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        // return author
        return Ok(_mapper.Map<AuthorDto>(authorFromRepo).ShepData(fields));
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
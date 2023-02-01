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
        IMapper mapper, IPropertyMappingService propertyMappingService, IPropertyCheckerService propertyCheckerService)
    {
        _courseLibraryRepository = courseLibraryRepository ??
                                   throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ??
                  throw new ArgumentNullException(nameof(mapper));
        _propertyMappingService = propertyMappingService ?? throw new ArgumentNullException(nameof(propertyMappingService));
        _propertyCheckerService = propertyCheckerService ?? throw new ArgumentNullException(nameof(propertyCheckerService));
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

        var paginationMetadata = new
        {
            totalCount = authorsFromRepo.TotalCount,
            pageSize = authorsFromRepo.PageSize,
            currentPage = authorsFromRepo.CurrentPage,
            totalPages = authorsFromRepo.TotalPages,
        };
        Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));

        var links = CreateLinksForAuthors(resourceParameters,authorsFromRepo.HasNext,authorsFromRepo.HasPrevious);

        var shapedAuthors = _mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo).ShepData(resourceParameters.Fields);

        var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
        {
            var authorAsDictionary = author as IDictionary<string, object>;
            var authorLinks = CreateLinksForAuthor((Guid)authorAsDictionary["Id"], null);
            authorAsDictionary.Add("links",authorLinks);
            return authorAsDictionary;
        });
        var linkedCollectionResource = new
        {
            value = shapedAuthorsWithLinks,
            links = links
        };
        
        return Ok(linkedCollectionResource);
    }

    private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorResourceParameters resourceParameters,bool hasNext, bool hasPrevious)
    {
        var links = new List<LinkDto> { new(CreateAuthorsResourceUri(resourceParameters, ResourceUriType.Current), rel: "self", "GET") };

        if (hasNext)
        {
            links.Add(new LinkDto(CreateAuthorsResourceUri(resourceParameters,ResourceUriType.NextPage),"nextPage","Get"));
        }
        if (hasPrevious)
        {
            links.Add(new LinkDto(CreateAuthorsResourceUri(resourceParameters,ResourceUriType.PreviousPage),"previousPage","Get"));
        }
        return links;
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
            ResourceUriType.Current => Url.Link("GetAuthors", new
            {
                fields = resourceParameters.Fields,
                orderBy = resourceParameters.OrderBy,
                pageNumber = resourceParameters.PageNumber,
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

    [HttpGet("{authorId}", Name = "GetAuthor")]
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

        var links = CreateLinksForAuthor(authorId, fields);
        
        var linkedResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo).ShepData(fields) as IDictionary<string, object?>;

        linkedResourceToReturn.Add("links", links);
        return Ok(linkedResourceToReturn);
    }

    private IEnumerable<LinkDto> CreateLinksForAuthor(Guid authorId,string? fields)
    {
        var links = new List<LinkDto>();

        if (string.IsNullOrWhiteSpace(fields))
        {
            links.Add(
                new LinkDto(Url.Link("GetAuthor", new { authorId }),
                    "self",
                    "GET"));
        }
        else
        {
            links.Add(
                new LinkDto(Url.Link("GetAuthor", new { authorId, fields }),
                    "self",
                    "GET"));
        }

        links.Add(
            new LinkDto(Url.Link("CreateCourseForAuthor", new { authorId }),
                "create_course_for_author",
                "POST"));
        links.Add(
            new LinkDto(Url.Link("GetCoursesForAuthor", new { authorId }),
                "courses",
                "GET"));

        return links;
    }

    [HttpPost(Name = "CreateAuthor")]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);
        var links = CreateLinksForAuthor(authorToReturn.Id, null);
        var linkedResourceToReturn = authorToReturn.ShepData(null) as IDictionary<string, object>;
        linkedResourceToReturn.Add("links", links);
        return CreatedAtRoute("GetAuthor",
            new { authorId = linkedResourceToReturn["Id"] },
            linkedResourceToReturn);
    }

    [HttpOptions]
    public IActionResult GetAuthorsOptions()
    {
        Response.Headers.Add("Allow", "GET,HEAD,POST,OPTIONS");
        return Ok();
    }
}
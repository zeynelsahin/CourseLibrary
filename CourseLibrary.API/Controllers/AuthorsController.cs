using System.Dynamic;
using System.Text.Json;
using AutoMapper;
using CourseLibrary.API.ActionConstraints;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Net.Http.Headers;

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

        var links = CreateLinksForAuthors(resourceParameters, authorsFromRepo.HasNext, authorsFromRepo.HasPrevious);

        var shapedAuthors = _mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo).ShepData(resourceParameters.Fields);

        var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
        {
            var authorAsDictionary = author as IDictionary<string, object>;
            var authorLinks = CreateLinksForAuthor((Guid)authorAsDictionary["Id"], null);
            authorAsDictionary.Add("links", authorLinks);
            return authorAsDictionary;
        });
        var linkedCollectionResource = new
        {
            value = shapedAuthorsWithLinks,
            links = links
        };

        return Ok(linkedCollectionResource);
    }

    private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorResourceParameters resourceParameters, bool hasNext, bool hasPrevious)
    {
        var links = new List<LinkDto> { new(CreateAuthorsResourceUri(resourceParameters, ResourceUriType.Current), rel: "self", "GET") };

        if (hasNext)
        {
            links.Add(new LinkDto(CreateAuthorsResourceUri(resourceParameters, ResourceUriType.NextPage), "nextPage", "Get"));
        }

        if (hasPrevious)
        {
            links.Add(new LinkDto(CreateAuthorsResourceUri(resourceParameters, ResourceUriType.PreviousPage), "previousPage", "Get"));
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

    [RequestHeaderMatchesMediaType("Accept","application/json","application/vnd.marvin.author.friendly+json")]
    [Produces("application/json","application/vnd.marvin.author.friendly+json")]
    [HttpGet("{authorId:guid}",Name = "GetAuthor")]
     public async Task<IActionResult> GetAuthorWithoutLinks(Guid authorId, string? fields)
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

        var friendlyResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo).ShapeData(fields);
        return Ok(friendlyResourceToReturn);
    }
     [RequestHeaderMatchesMediaType("Accept","application/vnd.marvin.hateoas+json","application/vnd.marvin.author.friendly.hateoas+json")]
     [Produces("application/vnd.marvin.hateoas+json","application/vnd.marvin.author.friendly.hateoas+json")]
     [HttpGet("{authorId:guid}",Name = "GetAuthor")]
     public async Task<IActionResult> GetAuthorWithLinks(Guid authorId, string? fields)
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

         IEnumerable<LinkDto> links = CreateLinksForAuthor(authorId, fields);
         
         var friendlyResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo).ShapeData(fields) as IDictionary<string,object?>;
         
         friendlyResourceToReturn.Add("links",links);
         
         return Ok(friendlyResourceToReturn);
     }
     
     [RequestHeaderMatchesMediaType("Accept","application/vnd.marvin.author.full+json")]
     [Produces("application/vnd.marvin.author.full+json")]
     [HttpGet("{authorId:guid}",Name = "GetAuthor")]
     public async Task<IActionResult> GetFullAuthorWithoutLinks(Guid authorId, string? fields)
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

         var fullResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo).ShapeData(fields);
         
         return Ok(fullResourceToReturn);
     }
     
     [RequestHeaderMatchesMediaType("Accept","application/vnd.marvin.author.full.hateoas+json")]
     [Produces("application/vnd.marvin.author.full.hateoas+json")]
     [HttpGet("{authorId:guid}",Name = "GetAuthor")]
     public async Task<IActionResult> GetFullAuthorWithLinks(Guid authorId, string? fields)
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

         IEnumerable<LinkDto> links = CreateLinksForAuthor(authorId,fields);
         
         var fullResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo).ShapeData(fields) as IDictionary<string,object?>;
         
         fullResourceToReturn.Add("links",links);
         return Ok(fullResourceToReturn);
     }
     
    // [Produces("application/vnd.marvin.hateoas+json",
    //     "application/vnd.marvin.author.full+json",
    //     "application/vnd.marvin.author.full.hateoas+json",
    //     "application/vnd.marvin.author.friendly+json",
    //     "application/vnd.marvin.author.friendly.hateoas+json")]
    //
    // [HttpGet("{authorId}", Name = "GetAuthor")]
    // public async Task<IActionResult> GetAuthor(Guid authorId, string? fields, [FromHeader(Name = "Accept")] string? mediaType)
    // {
    //     if (!MediaTypeHeaderValue.TryParse(mediaType, out var parsedMediaType))
    //     {
    //         return BadRequest(_problemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: 400, detail: $"Accept header media type value is not a valid media type."));
    //     }
    //
    //     if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
    //     {
    //         return BadRequest(_problemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: 400, detail: $"Not all requested data shaping fields exist on the resource: {fields}"));
    //     }
    //
    //     // get author from repo
    //     var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);
    //
    //     if (authorFromRepo == null)
    //     {
    //         return NotFound();
    //     }
    //
    //     var includeLinks = parsedMediaType.SubTypeWithoutSuffix.EndsWith("hateoas", StringComparison.CurrentCultureIgnoreCase);
    //
    //     IEnumerable<LinkDto> links = new List<LinkDto>();
    //
    //     if (includeLinks)
    //     {
    //         links = CreateLinksForAuthor(authorId, fields);
    //     }
    //
    //     var primaryMediaType = includeLinks ? parsedMediaType.SubTypeWithoutSuffix.Substring(0, parsedMediaType.SubTypeWithoutSuffix.Length - 8) : parsedMediaType.SubTypeWithoutSuffix;
    //
    //     //full
    //     if (primaryMediaType == "vnd.marvin.author.full")
    //     {
    //         var fullResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo).ShapeData(fields) as IDictionary<string, object>;
    //         if (includeLinks)
    //         {
    //             fullResourceToReturn.Add("links", links);
    //         }
    //
    //         return Ok(fullResourceToReturn);
    //     }
    //
    //     //friendly
    //
    //     var friendlyResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo).ShapeData(fields) as IDictionary<string, object?>;
    //     if (includeLinks)
    //     {
    //         friendlyResourceToReturn.Add("links", links);
    //     }
    //
    //     return Ok(friendlyResourceToReturn);
    //
    //     // return Ok(_mapper.Map<AuthorDto>(authorFromRepo));
    // }

    private IEnumerable<LinkDto> CreateLinksForAuthor(Guid authorId, string? fields)
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

    [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
    [RequestHeaderMatchesMediaType("Content-Type","application/vnd.marvin.authorforcreationwithdateofdeath+json")]
    [Consumes(("application/vnd.marvin.authorforcreationwithdateofdeath+json"))]
    public async Task<ActionResult<AuthorDto>> CreateAuthorWithDateOfDeath(AuthorForCreationWithDateOfDeathDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);
    
        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();
    
        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);
        var links = CreateLinksForAuthor(authorToReturn.Id, null);
        var linkedResourceToReturn = authorToReturn.ShapeData(null) as IDictionary<string, object>;
        linkedResourceToReturn.Add("links", links);
        return CreatedAtRoute("GetAuthor",
            new { authorId = linkedResourceToReturn["Id"] },
            linkedResourceToReturn);
    }
    
    [HttpPost(Name = "CreateAuthor")]
    [RequestHeaderMatchesMediaType("Content-Type","application/json","application/vnd.marvin.authorforcreation+json")]
    [Consumes("application/json","application/vnd.marvin.authorforcreation+json")]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);
        var links = CreateLinksForAuthor(authorToReturn.Id, null);
        var linkedResourceToReturn = authorToReturn.ShapeData(null) as IDictionary<string, object>;
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
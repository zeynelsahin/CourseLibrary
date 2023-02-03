﻿using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;

namespace CourseLibrary.API;

internal static class StartupHelperExtensions
{
    // Add services to the container
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers(configure =>
            {
                configure.ReturnHttpNotAcceptable = true; //sadece desteklenen formatda dönüş yapar aksi halde 406 Not Acceptable döner
                //configure.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter()); xml formatter
            })
            .AddNewtonsoftJson(options => { options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver(); })
            .AddXmlDataContractSerializerFormatters() // xml formatter;
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetailsFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var validationProblemDetails = problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);

                    validationProblemDetails.Detail = "See the errors field for details.";
                    validationProblemDetails.Instance = context.HttpContext.Request.Path;
                    validationProblemDetails.Type = "https://courselibrary/modelvalidationproblem";
                    validationProblemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                    validationProblemDetails.Title = "One or more validation errors occured.";
                    return new UnprocessableEntityObjectResult(validationProblemDetails)
                    {
                        ContentTypes = { "application/problem+json" },
                    };
                };
            });

        builder.Services.Configure<MvcOptions>(configure =>
        {
            var newtonSoftJsonOutputFormatter = configure.OutputFormatters.OfType<NewtonsoftJsonOutputFormatter>().FirstOrDefault();
            newtonSoftJsonOutputFormatter?.SupportedMediaTypes.Add("application/vnd.marvin.hateoas+json");
        });
        
        builder.Services.AddScoped<ICourseLibraryRepository,
            CourseLibraryRepository>();
        builder.Services.AddTransient<IPropertyMappingService,
            PropertyMappingService>();
        builder.Services.AddTransient<IPropertyCheckerService,
            PropertyCheckerService>();
        builder.Services.AddDbContext<CourseLibraryContext>(options => { options.UseSqlite(@"Data Source=library.db"); });

        builder.Services.AddAutoMapper(
            AppDomain.CurrentDomain.GetAssemblies());

        //Problem details Content-Type: application/problem+json
        return builder.Build();
    }

    // Configure the request/response pipeline
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler(builder => //application exception fırlattığında base message dönüyoruz
            {
                builder.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("An unexpected fault happened. Try again later");
                });
            });
        }

        app.UseAuthorization();

        app.MapControllers();

        return app;
    }

    public static async Task ResetDatabaseAsync(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var context = scope.ServiceProvider.GetService<CourseLibraryContext>();
                if (context != null)
                {
                    await context.Database.EnsureDeletedAsync();
                    await context.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
                logger.LogError(ex, "An error occurred while migrating the database.");
            }
        }
    }
}
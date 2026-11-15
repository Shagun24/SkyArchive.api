using System.Text.Json.Serialization;
using SpaceTrace.Backend.Data;
using SpaceTrace.Backend.Models;
using SpaceTrace.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:4200"];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IComponentCatalog, ComponentCatalog>();
builder.Services.AddHttpClient<INasaSourceRetriever, NasaSourceRetriever>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SpaceTraceAI/1.0 (NASA Space Apps Challenge)");
});
builder.Services.AddHttpClient<INasaImageLibraryClient, NasaImageLibraryClient>(client =>
{
    client.BaseAddress = new Uri("https://images-api.nasa.gov/");
    client.Timeout = TimeSpan.FromSeconds(12);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SpaceTraceAI/1.0 (NASA Space Apps Challenge)");
});
builder.Services.AddHttpClient<IAiStoryGenerator, AzureOpenAiStoryGenerator>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(45);
});
builder.Services.AddScoped<ISpaceAgentService, SpaceAgentService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

app.MapGet("/", () => Results.Ok(new
{
    application = "SpaceTrace AI Backend",
    version = "1.0.0",
    status = "running",
    endpoints = new[]
    {
        "GET /api/health",
        "GET /api/components",
        "GET /api/components/{id}",
        "GET /api/nasa/images?query=apollo%2011",
        "POST /api/agent/explore",
        "POST /api/agent/ask"
    }
}));

app.MapGet("/api/health", (IConfiguration configuration) => Results.Ok(new
{
    status = "healthy",
    utc = DateTimeOffset.UtcNow,
    aiConfigured = !string.IsNullOrWhiteSpace(configuration["AzureOpenAI:Endpoint"])
                   && !string.IsNullOrWhiteSpace(configuration["AzureOpenAI:ModelDeployment"])
                   && !string.IsNullOrWhiteSpace(configuration["AzureOpenAI:ApiKey"])
}));

app.MapGet("/api/components", (string? planet, IComponentCatalog catalog) =>
{
    var components = catalog.GetAll();
    if (!string.IsNullOrWhiteSpace(planet))
    {
        components = components
            .Where(component => component.Location.Equals(planet.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    return Results.Ok(components);
});

app.MapGet("/api/components/{id}", (string id, IComponentCatalog catalog) =>
{
    var component = catalog.GetById(id);
    IResult result = component is null
        ? Results.NotFound(new { error = $"Component '{id}' was not found." })
        : Results.Ok(component);
    return result;
});

app.MapGet("/api/nasa/images", async (
    string query,
    INasaImageLibraryClient nasaImages,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(query) || query.Length > 120)
        return (IResult)Results.BadRequest(new { error = "Provide a query between 1 and 120 characters." });

    var images = await nasaImages.SearchImagesAsync(query, 8, cancellationToken);
    return (IResult)Results.Ok(new { query, count = images.Count, images });
});

app.MapPost("/api/agent/explore", async (
    ExploreRequest request,
    ISpaceAgentService agent,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.ComponentId) || request.ComponentId.Length > 80)
        return (IResult)Results.BadRequest(new { error = "componentId is required and must be 80 characters or fewer." });

    var response = await agent.ExploreAsync(request, cancellationToken);
    IResult result = response is null
        ? Results.NotFound(new { error = $"No component was found for id '{request.ComponentId}'." })
        : Results.Ok(response);
    return result;
});

app.MapPost("/api/agent/ask", async (
    AskAgentRequest request,
    ISpaceAgentService agent,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.ComponentId) || string.IsNullOrWhiteSpace(request.Question))
        return (IResult)Results.BadRequest(new { error = "componentId and question are required." });
    if (request.Question.Length > 600)
        return (IResult)Results.BadRequest(new { error = "Question must be 600 characters or fewer." });

    var response = await agent.AskAsync(request, cancellationToken);
    IResult result = response is null
        ? Results.NotFound(new { error = $"No component was found for id '{request.ComponentId}'." })
        : Results.Ok(response);
    return result;
});

app.Run();

namespace SpaceTrace.Backend.Models;

public sealed record SpaceComponent(
    string Id,
    string Name,
    string Category,
    string Location,
    string Mission,
    int? MissionYear,
    string ShortDescription,
    string Purpose,
    string WhyLeftBehind,
    string LastKnownStatus,
    string SearchTerms,
    IReadOnlyList<SourceReference> Sources);

public sealed record SourceReference(
    string Title,
    string Url,
    string Provider,
    bool IsChallengeResource,
    string? Note = null);

public sealed record NasaImage(
    string NasaId,
    string Title,
    string? Description,
    string? DateCreated,
    string? PreviewImageUrl,
    string NasaItemUrl,
    string Provider = "NASA Image and Video Library");

public sealed record SourceExcerpt(
    string Title,
    string Url,
    string Excerpt,
    bool Retrieved);

public sealed record ExploreRequest(string ComponentId, string? Audience = "general", bool IncludeImages = true);

public sealed record StorySection(string Heading, string Body);

public sealed record ExploreResponse(
    SpaceComponent Component,
    string Summary,
    IReadOnlyList<StorySection> Story,
    IReadOnlyList<NasaImage> Images,
    IReadOnlyList<SourceReference> Sources,
    IReadOnlyList<SourceExcerpt> SourceExcerpts,
    string GenerationMode,
    string VerificationStatus,
    DateTimeOffset GeneratedAtUtc);

public sealed record AskAgentRequest(string ComponentId, string Question, string? Audience = "general");

public sealed record AskAgentResponse(
    string Answer,
    IReadOnlyList<AskSource> Sources);

public sealed record AskSource(string Title, string Url);

public sealed record StoryDraft(string Summary, List<StorySection> Story);

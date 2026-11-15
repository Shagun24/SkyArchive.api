using SpaceTrace.Backend.Data;
using SpaceTrace.Backend.Models;

namespace SpaceTrace.Backend.Services;

public interface ISpaceAgentService
{
    Task<ExploreResponse?> ExploreAsync(ExploreRequest request, CancellationToken cancellationToken);
    Task<AskAgentResponse?> AskAsync(AskAgentRequest request, CancellationToken cancellationToken);
}

public sealed class SpaceAgentService(
    IComponentCatalog catalog,
    INasaImageLibraryClient nasaImages,
    INasaSourceRetriever sourceRetriever,
    IAiStoryGenerator storyGenerator) : ISpaceAgentService
{
    internal const string MissingEvidenceMessage = "I couldn't find enough information in the NASA records to answer that question.";

    public async Task<ExploreResponse?> ExploreAsync(ExploreRequest request, CancellationToken cancellationToken)
    {
        var component = catalog.GetById(request.ComponentId);
        if (component is null) return null;

        var imageTask = request.IncludeImages
            ? nasaImages.SearchImagesAsync(component.SearchTerms, 8, cancellationToken)
            : Task.FromResult<IReadOnlyList<NasaImage>>([]);
        var excerptTask = sourceRetriever.FetchExcerptsAsync(component, cancellationToken);
        await Task.WhenAll(imageTask, excerptTask);
        var images = await imageTask;
        var excerpts = await excerptTask;
        var (draft, mode) = await storyGenerator.GenerateStoryAsync(
            component, images, excerpts, NormalizeAudience(request.Audience), cancellationToken);

        return new ExploreResponse(
            component,
            draft.Summary,
            draft.Story,
            images,
            component.Sources,
            excerpts,
            mode,
            "AI-generated narrative draft. Follow the linked NASA references; do not treat image search results as evidence of current equipment condition.",
            DateTimeOffset.UtcNow);
    }

    public async Task<AskAgentResponse?> AskAsync(AskAgentRequest request, CancellationToken cancellationToken)
    {
        var component = catalog.GetById(request.ComponentId);
        if (component is null) return null;

        var question = request.Question.Trim();
        var evidence = sourceRetriever.FindCatalogueEvidence(component, question);
        if (evidence.Count == 0)
            evidence = await sourceRetriever.FindExcerptsAsync(component, question, cancellationToken);

        if (evidence.Count == 0)
            return new AskAgentResponse(MissingEvidenceMessage, []);

        var (answer, sourceIds) = await storyGenerator.AnswerQuestionAsync(
            component, evidence, question, NormalizeAudience(request.Audience), cancellationToken);

        var sources = sourceIds.Select(id => evidence[id - 1])
            .DistinctBy(excerpt => excerpt.Url)
            .Select(excerpt => new AskSource(excerpt.Title, excerpt.Url))
            .ToArray();
        return new AskAgentResponse(answer, sources);
    }

    private static string NormalizeAudience(string? audience) =>
        string.IsNullOrWhiteSpace(audience) ? "general" : audience.Trim()[..Math.Min(audience.Trim().Length, 40)];
}

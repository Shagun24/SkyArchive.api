using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SpaceTrace.Backend.Data;
using SpaceTrace.Backend.Models;

namespace SpaceTrace.Backend.Services;

public interface INasaSourceRetriever
{
    Task<IReadOnlyList<SourceExcerpt>> FetchExcerptsAsync(SpaceComponent component, CancellationToken cancellationToken);
    IReadOnlyList<SourceExcerpt> FindCatalogueEvidence(SpaceComponent component, string question);
    Task<IReadOnlyList<SourceExcerpt>> FindExcerptsAsync(SpaceComponent component, string question, CancellationToken cancellationToken);
}

public sealed class NasaSourceRetriever(HttpClient httpClient, ILogger<NasaSourceRetriever> logger)
    : INasaSourceRetriever
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(12);
    private static readonly HashSet<string> CommonWords = new(
        "a an the is was were are it its in on to from of for as did does do can how what when where why who and or this that about me tell explain".Split(' '),
        StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<SourceExcerpt>> FetchExcerptsAsync(
        SpaceComponent component, CancellationToken cancellationToken)
    {
        var passages = await FetchPassagesAsync(component, cancellationToken);
        return passages.GroupBy(passage => passage.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First() with { Excerpt = string.Join(" ", group.Take(3).Select(passage => passage.Excerpt)) })
            .ToArray();
    }

    public IReadOnlyList<SourceExcerpt> FindCatalogueEvidence(SpaceComponent component, string question)
    {
        // These question patterns map to facts already verified for the catalogue.
        var text = question.ToLowerInvariant();
        var asksWhy = Regex.IsMatch(text, @"\b(why|cause[ds]?|reason)\b|\bwhat (happened|went wrong)\b");
        var aboutEnding = Regex.IsMatch(text, @"\b(stop(?:ped|ping)?|end(?:ed|ing)?|left|leave|abandon\w*|retir\w*|fail\w*|crash\w*)\b");
        var aboutDamage = Regex.IsMatch(text, @"\b(blades?|rotors?|damage[ds]?|crash\w*)\b");

        if (component.Id == "ingenuity-helicopter" && asksWhy &&
            (aboutEnding || aboutDamage || Regex.IsMatch(text, @"\b(final|last) flight\b")))
        {
            var investigation = component.Sources.Single(source => source.Url == ComponentCatalog.IngenuityInvestigationUrl);
            return [new SourceExcerpt(investigation.Title, investigation.Url,
                "Ingenuity stopped flying because its rotor blades were damaged during its final landing. NASA JPL investigators think the camera could not track enough features in the sand, which likely contributed to the hard landing.", true)];
        }

        var fact = asksWhy && aboutEnding
            ? component.WhyLeftBehind
            : Regex.IsMatch(text, @"\b(where)\b.*\b(now|today|remain\w*|located|rest\w*)\b")
                ? component.LastKnownStatus
                : Regex.IsMatch(text, @"\b(purpose|designed|built|sent)\b") &&
                  (asksWhy || Regex.IsMatch(text, @"\bwhat\b"))
                    ? component.Purpose
                    : null;

        if (fact is null) return [];
        var source = component.Sources.FirstOrDefault(item =>
            !item.Title.StartsWith("Phoenix & InSight", StringComparison.Ordinal));
        return source is null ? [] : [new SourceExcerpt(source.Title, source.Url, fact, true)];
    }

    public async Task<IReadOnlyList<SourceExcerpt>> FindExcerptsAsync(
        SpaceComponent component, string question, CancellationToken cancellationToken)
    {
        var componentWords = GetTerms($"{component.Name} {component.Mission} {component.Location}");
        var terms = GetTerms(question);
        terms.ExceptWith(componentWords);
        if (terms.Count == 0) return [];

        var passages = await FetchPassagesAsync(component, cancellationToken);
        return passages
            .Select(passage => (Passage: passage, Score: GetTerms(passage.Excerpt).Count(terms.Contains)))
            .Where(result => result.Score >= Math.Min(2, terms.Count))
            .OrderByDescending(result => result.Score)
            .Select(result => result.Passage)
            .DistinctBy(passage => passage.Excerpt)
            .Take(3)
            .ToArray();
    }

    private async Task<IReadOnlyList<SourceExcerpt>> FetchPassagesAsync(
        SpaceComponent component, CancellationToken cancellationToken)
    {
        var tasks = component.Sources
            .Where(source => IsAllowedNasaUrl(source.Url))
            .Where(source => !source.Title.StartsWith("Phoenix & InSight", StringComparison.Ordinal))
            .DistinctBy(source => source.Url)
            .Take(3)
            .Select(source => FetchOneAsync(source, cancellationToken));
        return (await Task.WhenAll(tasks)).SelectMany(passages => passages).ToArray();
    }

    private async Task<IReadOnlyList<SourceExcerpt>> FetchOneAsync(
        SourceReference source, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RequestTimeout);

        try
        {
            using var response = await httpClient.GetAsync(source.Url, timeout.Token);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(timeout.Token);
            return ExtractPassages(content, response.Content.Headers.ContentType?.MediaType)
                .Select(text => new SourceExcerpt(source.Title, source.Url, text, true))
                .ToArray();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogInformation(exception, "Could not retrieve NASA source {SourceUrl}", source.Url);
            return [];
        }
    }

    private static bool IsAllowedNasaUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return false;

        return (uri.Host == "nssdc.gsfc.nasa.gov" && uri.AbsolutePath.EndsWith("/display.action", StringComparison.OrdinalIgnoreCase))
            || (uri.Host == "science.nasa.gov" && uri.AbsolutePath.StartsWith("/mission/", StringComparison.OrdinalIgnoreCase))
            || value == ComponentCatalog.IngenuityInvestigationUrl;
    }

    private static IEnumerable<string> ExtractPassages(string content, string? mediaType)
    {
        if (mediaType?.Contains("xml", StringComparison.OrdinalIgnoreCase) == true ||
            content.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var xml = XDocument.Parse(content);
                var descriptions = xml.Descendants().Where(element =>
                    Regex.IsMatch(element.Name.LocalName, "description|abstract|summary|narrative", RegexOptions.IgnoreCase));
                var passages = descriptions
                    .SelectMany(element => Regex.Split(element.Value, @"\r?\n\s*\r?\n"))
                    .Select(NormalizeText).Where(IsUsablePassage).Select(LimitLength).Distinct().ToArray();
                if (passages.Length > 0) return passages;
            }
            catch (System.Xml.XmlException)
            {
                // Some archive responses contain HTML despite their XML content type.
            }
        }

        var html = Regex.Replace(content, @"(?is)<(script|style|noscript|svg|header|footer|nav|aside)\b[^>]*>.*?</\1\s*>", " ");
        var main = Regex.Match(html, @"(?is)<main\b[^>]*>(.*?)</main\s*>");
        var article = Regex.Match(html, @"(?is)<article\b[^>]*>(.*?)</article\s*>");
        var body = main.Success ? main.Groups[1].Value : article.Groups[1].Value;

        return Regex.Matches(body, @"(?is)<p\b[^>]*>(.*?)</p\s*>")
            .Select(match => NormalizeText(WebUtility.HtmlDecode(Regex.Replace(match.Groups[1].Value, "<[^>]+>", " "))))
            .Where(IsUsablePassage)
            .Select(LimitLength)
            .Distinct();
    }

    private static HashSet<string> GetTerms(string value) =>
        Regex.Matches(value.ToLowerInvariant(), @"[a-z0-9]+")
            .Select(match => match.Value)
            .Where(word => word.Length > 1 && !CommonWords.Contains(word))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static bool IsUsablePassage(string text) => text.Length >= 50;
    private static string LimitLength(string text) => text.Length > 650 ? text[..650] + "…" : text;
    private static string NormalizeText(string value) => Regex.Replace(value, @"\s+", " ").Trim();
}

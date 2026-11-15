using System.Text.Json;
using SpaceTrace.Backend.Models;

namespace SpaceTrace.Backend.Services;

public interface INasaImageLibraryClient
{
    Task<IReadOnlyList<NasaImage>> SearchImagesAsync(string query, int limit, CancellationToken cancellationToken);
}

public sealed class NasaImageLibraryClient(HttpClient httpClient, ILogger<NasaImageLibraryClient> logger)
    : INasaImageLibraryClient
{
    public async Task<IReadOnlyList<NasaImage>> SearchImagesAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit, 1, 12);
        var url = $"search?q={Uri.EscapeDataString(query)}&media_type=image&page_size={safeLimit}";

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("collection", out var collection) ||
                !collection.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                return [];

            var results = new List<NasaImage>();
            foreach (var item in items.EnumerateArray())
            {
                var data = item.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array
                    ? dataArray.EnumerateArray().FirstOrDefault()
                    : default;

                if (data.ValueKind != JsonValueKind.Object) continue;

                var nasaId = GetString(data, "nasa_id");
                var title = GetString(data, "title");
                if (string.IsNullOrWhiteSpace(nasaId) || string.IsNullOrWhiteSpace(title)) continue;

                string? preview = null;
                if (item.TryGetProperty("links", out var links) && links.ValueKind == JsonValueKind.Array)
                {
                    foreach (var link in links.EnumerateArray())
                    {
                        if (string.Equals(GetString(link, "rel"), "preview", StringComparison.OrdinalIgnoreCase))
                        {
                            preview = GetString(link, "href");
                            break;
                        }
                    }
                }

                results.Add(new NasaImage(
                    nasaId,
                    title,
                    GetString(data, "description"),
                    GetString(data, "date_created"),
                    preview,
                    $"https://images.nasa.gov/details-{Uri.EscapeDataString(nasaId)}"));

                if (results.Count >= safeLimit) break;
            }

            return results;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "NASA Image Library search failed for query {Query}", query);
            return [];
        }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
}

# SpaceTrace AI – Backend starter

ASP.NET Core Web API for the NASA Space Apps Challenge project **Abandoned but not Forgotten**.

## What is included

- Component catalogue seeded from the 2026 challenge resource list (NSSDCA and NASA Science mission pages).
- `POST /api/agent/explore`: the click-to-explore workflow.
- Retrieval of short passages from the official NASA source pages attached to each component.
- NASA Image and Video Library search using `images-api.nasa.gov`.
- Azure OpenAI story generation using the current Azure OpenAI v1 Chat Completions route.
- Safe demo fallback when Azure credentials are missing or the model call fails.
- `POST /api/agent/ask` for follow-up questions.
- CORS configured for Angular at `http://localhost:4200`.

## Requirements

- .NET 10 SDK
- Network access to `images-api.nasa.gov`
- Optional: Azure OpenAI resource with a deployed chat model

## Run locally

```bash
cd SpaceTrace.Backend
dotnet restore
dotnet run
```

The launch profile listens on `http://localhost:5080`. In Development, open `http://localhost:5080/swagger` to explore and call the API. The OpenAPI document is available at `http://localhost:5080/swagger/v1/swagger.json`.

Check health:

```bash
curl http://localhost:5080/api/health
```

## Configure Azure OpenAI (optional)

Set these environment variables before running the application. Do not commit API keys.

PowerShell:

```powershell
$env:AzureOpenAI__Endpoint = "https://YOUR-RESOURCE.openai.azure.com"
$env:AzureOpenAI__ModelDeployment = "YOUR-DEPLOYMENT-NAME"
$env:AzureOpenAI__ApiKey = "YOUR-KEY"
dotnet run
```

Bash:

```bash
export AzureOpenAI__Endpoint="https://YOUR-RESOURCE.openai.azure.com"
export AzureOpenAI__ModelDeployment="YOUR-DEPLOYMENT-NAME"
export AzureOpenAI__ApiKey="YOUR-KEY"
dotnet run
```

The backend calls `POST {endpoint}/openai/v1/chat/completions` and sends the deployment name as `model`. If these settings are not present, the API still runs and returns a deterministic demo story from the catalogue.

## Main API

### `GET /api/components`

Returns all known components. Optional `?planet=Moon` or `?planet=Mars` filter.

### `GET /api/components/{id}`

Returns a single component record, including challenge resource links.

### `POST /api/agent/explore`

Request:

```json
{
  "componentId": "apollo-11-descent-stage",
  "audience": "students aged 10-15",
  "includeImages": true
}
```

The orchestrator looks up the component, retrieves NASA mission-page excerpts and NASA images in parallel, asks the configured AI model for a story grounded in that retrieved context, and returns the story, images, excerpts and source links.

### `POST /api/agent/ask`

Request:

```json
{
  "componentId": "apollo-11-descent-stage",
  "question": "Why was the descent stage left on the Moon?",
  "audience": "students aged 10-15"
}
```

The response contains `answer` and `sources`. Each source contains `title` and `url`. The backend checks curated catalogue facts first, then looks for relevant paragraphs in the linked NASA sources. If Azure OpenAI is unavailable, it returns a matching passage with its source. If the records do not answer the question, it returns an explanatory message and an empty source list. The explore endpoint retains its existing response shape.

### `GET /api/nasa/images?query=apollo%2011`

Searches NASA's Image and Video Library directly.

## Angular integration

Call the explore API when the Three.js raycaster returns a component ID:

```typescript
this.http.post<ExploreResponse>(
  'http://localhost:5080/api/agent/explore',
  { componentId: clickedObject.userData['componentId'], audience: 'students aged 10-15', includeImages: true }
);
```

Keep component IDs in the 3D model metadata (`userData.componentId`) and use the IDs returned by `GET /api/components`.

## Important data-integrity notes

- The catalogue uses the resources shown for the challenge, including NSSDCA and NASA Science links. Keep those links visible in the UI.
- NASA Image Library search results are supporting media, not proof of an artifact's current physical condition.
- The AI narrative is marked as a draft. Verify every factual statement against the linked NASA sources before presenting it as confirmed.
- Some archive pages are historical records. The API does not perform live telemetry or remote condition monitoring.
- NASA source inclusion and citation is a challenge requirement. Confirm the exact resource list in the official challenge page before final submission.

## Suggested next backend increment

Replace the seed catalogue with a small database and add an NSSDCA ingestion worker that stores source records, retrieval date, extracted text and provenance. Then use Azure AI Search for retrieval over those records. For the hackathon demo, this starter keeps the critical click-to-story path small and runnable.

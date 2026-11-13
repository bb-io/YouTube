using System.Net;
using System.Text.Json;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using RestSharp;

namespace Apps.YouTube;
public sealed class YouTubeClient : BlackBirdRestClient
{
    private static readonly HttpClient SharedClient = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5), AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromHours(4)
    };
    private readonly HttpClient client;
    private readonly bool customTransport;
    private readonly string apiKey;
    private readonly string authorization;
    public YouTubeClient(IEnumerable<AuthenticationCredentialsProvider> credentials, HttpClient? httpClient = null, HttpMessageHandler? restHandler = null) : base(new RestClientOptions("https://www.googleapis.com/youtube/v3/") { FollowRedirects = false, MaxTimeout = 60000, ConfigureMessageHandler = restHandler == null ? null : _ => restHandler })
    {
        var creds = credentials.ToList();
        apiKey = creds.FirstOrDefault(x => x.KeyName == "api_key")?.Value?.Trim() ?? string.Empty;
        authorization = creds.FirstOrDefault(x => x.KeyName == "Authorization")?.Value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey) && string.IsNullOrWhiteSpace(authorization))
            throw new PluginMisconfigurationException("Create a YouTube API-key connection for public discovery, or an OAuth connection for channel management.");
        client = httpClient ?? SharedClient;
        customTransport = httpClient != null;
    }

    public override Task<RestResponse> ExecuteWithErrorHandling(RestRequest request) => RunAsync(async () =>
    {
        if (request.Parameters.Any(p => p.Type == ParameterType.GetOrPost))
            throw new PluginMisconfigurationException("Use query parameters and a JSON request body for this operation.");
        var headers = request.Parameters.Where(p => p.Type == ParameterType.HttpHeader).ToDictionary(p => p.Name!, p => p.Value?.ToString() ?? "", StringComparer.OrdinalIgnoreCase);
        if (headers.Keys.Any(h => new[] { "Authorization", "Host", "Cookie", "Proxy-Authorization", "X-Goog-Api-Key", "Content-Length", "Transfer-Encoding" }.Contains(h, StringComparer.OrdinalIgnoreCase)))
            throw new PluginMisconfigurationException("Authentication and transport headers are supplied by the connection and cannot be overridden.");
        if (request.Method != Method.Get)
            RequireOAuth();
        var parameter = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
        var payload = parameter?.Value is string text ? text : parameter == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(parameter.Value);
        using var body = payload == null ? null : new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        using var response = await SendAsync(new HttpMethod(request.Method.ToString().ToUpperInvariant()), this.BuildUri(request).ToString(), [], body, headers: headers);
        return new RestResponse(request)
        {
            StatusCode = response.StatusCode,
            Content = await response.Content.ReadAsStringAsync(),
            ResponseStatus = ResponseStatus.Completed
        };
    });
    public static async Task<T> RunAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (PluginMisconfigurationException)
        {
            throw;
        }
        catch (PluginApplicationException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)when (ex is not OutOfMemoryException)
        {
            throw new PluginApplicationException("The YouTube operation could not be completed. Check service availability and file access. For writes, inspect the resource before retrying.");
        }
    }

    public Task<T> GetAsync<T>(string resource, Dictionary<string, string> parameters) => RunAsync(async () =>
    {
        using var response = await SendAsync(HttpMethod.Get, resource, parameters);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync()) ?? throw new PluginApplicationException("YouTube returned an empty response. Retry the operation.");
    });
    public async Task<List<T>> Paginate<T>(string resource, Dictionary<string, string> parameters, int maximumResults = int.MaxValue, Func<IReadOnlyList<T>, Task<bool>>? continueAfterPage = null, Action? beforePage = null, bool collectItems = true)
    {
        var query = new Dictionary<string, string>(parameters);
        var items = new List<T>();
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        for (var page = 0; page < 1000; page++)
        {
            beforePage?.Invoke();
            List<T> pageItems;
            string nextPageToken;
            if (typeof(T) == typeof(JsonElement))
            {
                var data = await GetAsync(resource, query);
                pageItems = Items(data).Cast<T>().ToList();
                nextPageToken = Text(data, "nextPageToken");
            }
            else
            {
                var data = await GetAsync<YouTubePage<T>>(resource, query);
                pageItems = data.Items;
                nextPageToken = data.NextPageToken ?? "";
            }

            if (collectItems)
                items.AddRange(pageItems.Take(maximumResults - items.Count));
            if (continueAfterPage != null && !await continueAfterPage(pageItems))
                return items;
            if (string.IsNullOrEmpty(nextPageToken) || items.Count >= maximumResults)
                return items;
            if (!tokens.Add(nextPageToken))
                throw new PluginApplicationException("YouTube repeated a pagination token. No polling memory was advanced.");
            query["pageToken"] = nextPageToken;
        }

        throw new PluginApplicationException("YouTube exceeded 1,000 pages. Narrow the source before retrying.");
    }

    public Task<List<T>> Paginate<T>(RestRequest request, int maximumResults = int.MaxValue, Func<IReadOnlyList<T>, Task<bool>>? continueAfterPage = null, Action? beforePage = null, bool collectItems = true) => Paginate(this.BuildUri(request).ToString(), new Dictionary<string, string>(), maximumResults, continueAfterPage, beforePage, collectItems);
    public bool HasOAuth => !string.IsNullOrEmpty(authorization);

    public string Redact(string value)
    {
        foreach (var secret in new[]
        {
            apiKey,
            authorization,
            authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..] : ""
        }

        )
            if (!string.IsNullOrEmpty(secret))
                value = value.Replace(secret, "[redacted]", StringComparison.Ordinal);
        return value;
    }

    public void RequireOAuth()
    {
        if (!HasOAuth)
            throw new PluginMisconfigurationException("This action requires the YouTube channel management OAuth connection. An API key cannot authorize caption access or video updates.");
    }

    public async Task<JsonElement> GetAsync(string resource, Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Get, resource, parameters, ct: ct);
        return await ReadJsonAsync(response, ct);
    }

    protected override Exception ConfigureErrorException(RestResponse response) => CreateError(response.StatusCode, response.Content ?? "", response.ResponseUri?.ToString() ?? "");
    private Exception CreateError(HttpStatusCode status, string text, string resource)
    {
        string reason = "";
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.Object && err.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
                reason = Text(errors[0], "reason");
        }
        catch (JsonException)
        {
        }

        if (reason is "quotaExceeded" or "dailyLimitExceeded")
            return new PluginMisconfigurationException("The Google Cloud project's YouTube API quota is exhausted. Wait for the quota to reset or increase its quota.");
        if (status == HttpStatusCode.PreconditionFailed)
            return new PluginApplicationException("The resource changed while it was being updated. Read its current state and retry the update.");
        if (status == HttpStatusCode.Conflict)
            return new PluginMisconfigurationException("YouTube reported a conflicting resource. For captions, search existing tracks and update the matching language/name. Check the current resource before retrying.");
        if (resource.Contains("youtubeanalytics.googleapis.com", StringComparison.Ordinal) && status is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
            return new PluginMisconfigurationException("Enable YouTube Analytics API and reconnect OAuth to grant YouTube Analytics and YouTube read-only permissions. For revenue metrics, choose Include revenue reports when reconnecting. Check that this account owns the requested report data.");
        if (reason is "defaultLanguageNotSet" or "defaultLanguageNotSetError")
            return new PluginMisconfigurationException("Set the video's source metadata language using Default language and retry.");
        if (reason == "commentsDisabled")
            return new PluginMisconfigurationException("Comments are disabled for this video. Enable comments or choose another video.");
        if (reason == "liveStreamingNotEnabled")
            return new PluginMisconfigurationException("Enable live streaming for this YouTube channel before using the owned-channel livestream event.");
        if (reason == "couldNotConvert")
            return new PluginMisconfigurationException("YouTube could not convert the caption track. Choose a supported format and check that the track status is serving.");
        if (status is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return new PluginMisconfigurationException(HasOAuth ? "YouTube rejected the request. Check the action inputs and reconnect with the requested management permission using the account authorized to edit this video." : "YouTube rejected the request. Check the API key, enable YouTube Data API v3, and check the key restrictions.");
        if (reason == "invalidPublishAt")
            return new PluginMisconfigurationException("Choose a future Publish at date for an already private video that has never been published. Remove the schedule to publish immediately.");
        if (reason is "invalidTitle" or "playlistTitleRequired" or "invalidDescription" or "invalidTags" or "invalidCategoryId")
            return new PluginMisconfigurationException("Check the title, description, tags and category. Choose a valid category and keep text within the documented limits.");
        if (reason is "playlistOperationUnsupported" or "playlistItemsNotAccessible")
            return new PluginMisconfigurationException("This playlist cannot be changed through the API. Choose an editable playlist owned by the connected account.");
        if (status == HttpStatusCode.NotFound)
            return new PluginMisconfigurationException("The requested video, caption, playlist or playlist entry was not found or is not accessible with this connection. Check the input and channel permissions.");
        return new PluginApplicationException($"YouTube returned HTTP {(int)status}. For writes, check the current state before retrying.");
    }

    internal async Task<JsonElement> WriteAsync(HttpMethod method, string resource, Dictionary<string, string> parameters, HttpContent body, string? etag = null)
    {
        RequireOAuth();
        using var response = await SendAsync(method, resource, parameters, body, etag);
        return await ReadJsonAsync(response);
    }

    internal async Task<HttpResponseMessage> SendAsync(HttpMethod method, string resource, Dictionary<string, string> parameters, HttpContent? body = null, string? etag = null, CancellationToken ct = default, Dictionary<string, string>? headers = null, bool stopOnError = true, bool streamResponse = false)
    {
        var url = ApiUrls.Build(resource, parameters, HasOAuth ? null : apiKey);
        for (var attempt = 0;; attempt++)
        {
            using var request = new HttpRequestMessage(method, url);
            if (HasOAuth)
                request.Headers.Authorization = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(authorization);
            if (!string.IsNullOrWhiteSpace(etag))
                request.Headers.TryAddWithoutValidation("If-Match", etag);
            request.Content = body;
            if (headers != null)
                foreach (var h in headers)
                    request.Headers.TryAddWithoutValidation(h.Key, h.Value);
            HttpResponseMessage response;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(body is StreamContent ? TimeSpan.FromHours(4) : TimeSpan.FromSeconds(60));
            try
            {
                if (customTransport || streamResponse || body is StreamContent or MultipartContent)
                    response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                else
                {
                    var rest = new RestRequest(url, Enum.Parse<Method>(method.Method, true));
                    if (HasOAuth)
                        rest.AddHeader("Authorization", authorization);
                    if (!string.IsNullOrWhiteSpace(etag))
                        rest.AddHeader("If-Match", etag);
                    if (headers != null)
                        foreach (var header in headers)
                            rest.AddHeader(header.Key, header.Value);
                    if (body != null)
                        rest.AddStringBody(await body.ReadAsStringAsync(timeout.Token), body.Headers.ContentType?.ToString() ?? "application/json");
                    var result = await this.ExecuteAsync(rest, timeout.Token);
                    if ((int)result.StatusCode == 0)
                        throw new PluginApplicationException("YouTube could not be reached or timed out. For writes, check the current resource before retrying.");
                    response = new HttpResponseMessage(result.StatusCode)
                    {
                        Content = new ByteArrayContent(result.RawBytes ?? System.Text.Encoding.UTF8.GetBytes(result.Content ?? ""))
                    };
                    if (result.Headers != null)
                        foreach (var header in result.Headers)
                            if (header.Name != null)
                                response.Headers.TryAddWithoutValidation(header.Name, header.Value?.ToString());
                    if (result.ContentType != null)
                        response.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(result.ContentType);
                }
            }
            catch (OperationCanceledException)when (!ct.IsCancellationRequested)
            {
                throw new PluginApplicationException("YouTube timed out. For a write operation, check the video's current state before retrying.");
            }
            catch (HttpRequestException ex)
            {
                if (ex.GetBaseException()is PluginMisconfigurationException inputError)
                    throw inputError;
                throw new PluginApplicationException("YouTube could not be reached. For a write operation, check the video's current state before retrying.");
            }

            if (response.IsSuccessStatusCode || !stopOnError)
                return response;
            if (method == HttpMethod.Get && (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500) && attempt < 2)
            {
                var delay = response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow) ?? TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
                response.Dispose();
                if (delay > TimeSpan.FromSeconds(30))
                    throw new PluginApplicationException("YouTube is rate limiting requests. Retry after the current limit expires.");
                await Task.Delay(delay < TimeSpan.Zero ? TimeSpan.Zero : delay, ct);
                continue;
            }

            using (response)
            {
                var text = await response.Content.ReadAsStringAsync(ct);
                throw CreateError(response.StatusCode, text, resource);
            }
        }
    }

    internal static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.Clone();
        }
        catch (Exception ex)when (ex is JsonException or IOException or HttpRequestException)
        {
            throw new PluginApplicationException("YouTube returned an unreadable response. For writes, check the current state before retrying.");
        }
    }

    internal static List<JsonElement> Items(JsonElement data) => data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array ? items.EnumerateArray().ToList() : throw new PluginApplicationException("YouTube's response did not contain an items collection. Retry the operation.");
    internal static string Text(JsonElement data, string property) => data.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
}

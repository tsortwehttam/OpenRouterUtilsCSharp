using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRouterUtils;

public interface IOpenRouterTransport
{
    Task<OpenRouterTransportResponse> SendAsync(
        OpenRouterTransportRequest request,
        CancellationToken cancellationToken);
}

public sealed class OpenRouterTransportRequest
{
    public OpenRouterTransportRequest(
        string method,
        Uri uri,
        IReadOnlyDictionary<string, string> headers,
        string body)
    {
        Method = method;
        Uri = uri;
        Headers = headers;
        Body = body;
    }

    public string Method { get; }

    public Uri Uri { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public string Body { get; }
}

public sealed class OpenRouterTransportResponse
{
    public OpenRouterTransportResponse(int statusCode, string body)
    {
        StatusCode = statusCode;
        Body = body;
    }

    public int StatusCode { get; }

    public string Body { get; }
}

public sealed class HttpClientOpenRouterTransport : IOpenRouterTransport, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeClient;

    public HttpClientOpenRouterTransport()
        : this(new HttpClient(), true)
    {
    }

    public HttpClientOpenRouterTransport(HttpClient httpClient, bool disposeClient = false)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeClient = disposeClient;
    }

    public async Task<OpenRouterTransportResponse> SendAsync(
        OpenRouterTransportRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(new HttpMethod(request.Method), request.Uri);
        foreach (var header in request.Headers)
        {
            message.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        message.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return new OpenRouterTransportResponse((int)response.StatusCode, body);
    }

    public void Dispose()
    {
        if (_disposeClient)
        {
            _httpClient.Dispose();
        }
    }
}

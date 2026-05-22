using System;
using System.Collections.Generic;

namespace OpenRouterUtils;

public sealed class OpenRouterClientOptions
{
    public OpenRouterClientOptions(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("An OpenRouter API key is required.", nameof(apiKey));
        }

        ApiKey = apiKey;
    }

    public string ApiKey { get; }

    public Uri BaseUri { get; set; } = new Uri(OpenRouterConstants.DefaultBaseUrl);

    public string? Referer { get; set; }

    public string? Title { get; set; }

    public IOpenRouterTransport? Transport { get; set; }

    public bool DisposeTransport { get; set; }

    public IList<string> DefaultChatModels { get; } = new List<string> { OpenRouterConstants.DefaultChatModel };

    public IList<string> DefaultRawCompletionModels { get; } = new List<string> { OpenRouterConstants.DefaultRawCompletionModel };

    public IList<string> DefaultImageModels { get; } = new List<string> { OpenRouterConstants.DefaultImageModel };
}

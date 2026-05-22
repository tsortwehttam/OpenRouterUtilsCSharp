using System.Collections.Generic;

namespace OpenRouterUtils;

/// <summary>
/// Optional parameters for chat completion requests.
/// </summary>
public sealed class ChatRequestOptions
{
    public int? MaxTokens { get; set; }

    public double? Temperature { get; set; }

    public double? TopP { get; set; }

    public int? Seed { get; set; }

    public IList<string> Stop { get; } = new List<string>();

    public OpenRouterResponseFormat? ResponseFormat { get; set; }
}

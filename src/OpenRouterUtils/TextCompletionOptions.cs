using System.Collections.Generic;

namespace OpenRouterUtils;

/// <summary>
/// Optional parameters for simple text completion via chat models.
/// </summary>
public sealed class TextCompletionOptions
{
    public int? MaxTokens { get; set; }

    public double? Temperature { get; set; }

    public double? TopP { get; set; }

    public int? Seed { get; set; }

    public IList<string> Stop { get; } = new List<string>();
}

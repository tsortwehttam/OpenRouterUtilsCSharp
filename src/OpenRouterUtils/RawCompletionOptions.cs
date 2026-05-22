using System.Collections.Generic;

namespace OpenRouterUtils;

/// <summary>
/// Optional parameters for the legacy/base-model completions endpoint.
/// </summary>
public sealed class RawCompletionOptions
{
    public int? MaxTokens { get; set; }

    public double? Temperature { get; set; }

    public double? TopP { get; set; }

    public int? Seed { get; set; }

    public IList<string> Stop { get; } = new List<string>();
}

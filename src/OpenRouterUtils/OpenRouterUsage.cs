namespace OpenRouterUtils;

public sealed class OpenRouterUsage
{
    public OpenRouterUsage(int? promptTokens, int? completionTokens, int? totalTokens)
    {
        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
        TotalTokens = totalTokens;
    }

    public int? PromptTokens { get; }

    public int? CompletionTokens { get; }

    public int? TotalTokens { get; }
}

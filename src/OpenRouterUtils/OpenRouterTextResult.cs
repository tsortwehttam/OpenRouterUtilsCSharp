namespace OpenRouterUtils;

public sealed class OpenRouterTextResult
{
    public OpenRouterTextResult(string text, string? model, string? id, string? finishReason, OpenRouterUsage? usage)
    {
        Text = text;
        Model = model;
        Id = id;
        FinishReason = finishReason;
        Usage = usage;
    }

    public string Text { get; }

    public string? Model { get; }

    public string? Id { get; }

    public string? FinishReason { get; }

    public OpenRouterUsage? Usage { get; }
}

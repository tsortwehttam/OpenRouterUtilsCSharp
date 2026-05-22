namespace OpenRouterUtils;

/// <summary>
/// A chat message sent to, or received from, a chat model.
/// </summary>
public sealed class ChatMessage
{
    public ChatMessage(ChatMessageRole role, string content)
    {
        Role = role;
        Content = content ?? string.Empty;
    }

    public ChatMessageRole Role { get; }

    public string Content { get; }

    public static ChatMessage System(string content)
    {
        return new ChatMessage(ChatMessageRole.System, content);
    }

    public static ChatMessage Developer(string content)
    {
        return new ChatMessage(ChatMessageRole.Developer, content);
    }

    public static ChatMessage User(string content)
    {
        return new ChatMessage(ChatMessageRole.User, content);
    }

    public static ChatMessage Assistant(string content)
    {
        return new ChatMessage(ChatMessageRole.Assistant, content);
    }
}

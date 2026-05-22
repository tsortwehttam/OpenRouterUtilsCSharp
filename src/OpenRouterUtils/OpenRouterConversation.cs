using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRouterUtils;

public sealed class OpenRouterConversation
{
    private readonly OpenRouterClient _client;
    private readonly IReadOnlyList<string> _models;
    private readonly ChatRequestOptions? _options;
    private readonly List<ChatMessage> _messages = new List<ChatMessage>();

    internal OpenRouterConversation(
        OpenRouterClient client,
        IReadOnlyList<string> models,
        ChatMessage? systemMessage,
        ChatRequestOptions? options)
    {
        _client = client;
        _models = models;
        _options = options;
        if (systemMessage != null)
        {
            _messages.Add(systemMessage);
        }
    }

    public IReadOnlyList<ChatMessage> Messages => _messages;

    public void Add(ChatMessage message)
    {
        _messages.Add(message);
    }

    public async Task<string> SendAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        _messages.Add(ChatMessage.User(userMessage));
        var response = await _client.ChatTextAsync(_messages, _models, _options, cancellationToken).ConfigureAwait(false);
        _messages.Add(ChatMessage.Assistant(response));
        return response;
    }
}

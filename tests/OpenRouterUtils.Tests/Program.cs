using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenRouterUtils;

await Tests.RunAsync();

internal static class Tests
{
    public static async Task RunAsync()
    {
        await SendsFriendlyChatRequestAsync();
        await FallsBackOnUnavailableModelAsync();
        await ReadsRawCompletionTextAsync();
        await ReadsGeneratedImageAsync();
        await ConversationStoresHistoryAsync();
        Console.WriteLine("All tests passed.");
    }

    private static async Task SendsFriendlyChatRequestAsync()
    {
        var transport = new QueueTransport(
            new OpenRouterTransportResponse(200, ChatResponse("hello", "openai/gpt-4.1-mini")));
        var client = Client(transport);

        var text = await client.ChatTextAsync(
            new[]
            {
                ChatMessage.Developer("Be terse."),
                ChatMessage.User("Hi")
            },
            "openai/gpt-4.1-mini",
            new ChatRequestOptions { Temperature = 0.25, Seed = 123 });

        AssertEqual("hello", text, "chat text");
        AssertEqual("POST", transport.Requests[0].Method, "method");
        AssertEqual("Bearer test-key", transport.Requests[0].Headers["Authorization"], "authorization");
        using var body = JsonDocument.Parse(transport.Requests[0].Body);
        AssertEqual("openai/gpt-4.1-mini", body.RootElement.GetProperty("model").GetString(), "model");
        AssertEqual("system", body.RootElement.GetProperty("messages")[0].GetProperty("role").GetString(), "developer role");
        AssertEqual(123, body.RootElement.GetProperty("seed").GetInt32(), "seed");
    }

    private static async Task FallsBackOnUnavailableModelAsync()
    {
        var transport = new QueueTransport(
            new OpenRouterTransportResponse(404, "{\"error\":{\"message\":\"missing\"}}"),
            new OpenRouterTransportResponse(200, ChatResponse("fallback", "openai/gpt-4o-mini")));
        var client = Client(transport);

        var text = await client.CompleteTextAsync("Say hi", new[] { "bad/model", "openai/gpt-4o-mini" });

        AssertEqual("fallback", text, "fallback text");
        AssertEqual(2, transport.Requests.Count, "fallback request count");
    }

    private static async Task ReadsRawCompletionTextAsync()
    {
        var transport = new QueueTransport(
            new OpenRouterTransportResponse(200, "{\"id\":\"cmpl_1\",\"model\":\"base\",\"choices\":[{\"text\":\" world\",\"finish_reason\":\"stop\"}]}"));
        var client = Client(transport);

        var text = await client.CompleteRawAsync("hello", "base", new RawCompletionOptions { MaxTokens = 5 });

        AssertEqual(" world", text, "raw text");
        AssertContains("/completions", transport.Requests[0].Uri.ToString(), "raw endpoint");
    }

    private static async Task ReadsGeneratedImageAsync()
    {
        const string dataUrl = "data:image/png;base64,aGVsbG8=";
        var body = "{\"choices\":[{\"message\":{\"images\":[{\"image_url\":{\"url\":\"" + dataUrl + "\"}}]}}]}";
        var transport = new QueueTransport(new OpenRouterTransportResponse(200, body));
        var client = Client(transport);

        var image = await client.GenerateImageAsync("shop", "image-model", new ImageGenerationOptions { AspectRatio = ImageAspectRatio.Wide16x9 });

        AssertEqual("image/png", image.MimeType, "mime");
        AssertEqual("aGVsbG8=", image.Base64, "base64");
        AssertEqual(5, image.GetBytes().Length, "bytes");
        AssertContains("16:9", transport.Requests[0].Body, "aspect ratio");
    }

    private static async Task ConversationStoresHistoryAsync()
    {
        var transport = new QueueTransport(
            new OpenRouterTransportResponse(200, ChatResponse("one", "model")),
            new OpenRouterTransportResponse(200, ChatResponse("two", "model")));
        var client = Client(transport);
        var conversation = client.CreateConversation("model", ChatMessage.System("NPC"));

        await conversation.SendAsync("a");
        await conversation.SendAsync("b");

        AssertEqual(5, conversation.Messages.Count, "conversation history");
        AssertEqual("two", conversation.Messages[4].Content, "last assistant");
    }

    private static OpenRouterClient Client(QueueTransport transport)
    {
        return new OpenRouterClient(new OpenRouterClientOptions("test-key")
        {
            Transport = transport,
            Referer = "https://example.test",
            Title = "OpenRouterUtils tests"
        });
    }

    private static string ChatResponse(string text, string model)
    {
        return "{\"id\":\"chat_1\",\"model\":\"" + model + "\",\"choices\":[{\"message\":{\"content\":\"" + text + "\"},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":1,\"completion_tokens\":2,\"total_tokens\":3}}";
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(name + ": expected " + expected + ", got " + actual);
        }
    }

    private static void AssertContains(string needle, string haystack, string name)
    {
        if (haystack.IndexOf(needle, StringComparison.Ordinal) < 0)
        {
            throw new InvalidOperationException(name + ": missing " + needle);
        }
    }
}

internal sealed class QueueTransport : IOpenRouterTransport
{
    private readonly Queue<OpenRouterTransportResponse> _responses;

    public QueueTransport(params OpenRouterTransportResponse[] responses)
    {
        _responses = new Queue<OpenRouterTransportResponse>(responses);
    }

    public List<OpenRouterTransportRequest> Requests { get; } = new List<OpenRouterTransportRequest>();

    public Task<OpenRouterTransportResponse> SendAsync(
        OpenRouterTransportRequest request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No queued response.");
        }

        return Task.FromResult(_responses.Dequeue());
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenRouterUtils;

var transport = new SmokeTransport();
var client = new OpenRouterClient(new OpenRouterClientOptions("test-key") { Transport = transport });

await client.CompleteTextAsync("hello", "chat-model");
await client.ChatTextAsync(new[] { ChatMessage.System("system"), ChatMessage.User("hello") }, "chat-model");
await client.CompleteRawAsync("raw", "raw-model");
await client.GenerateImageAsync("image", "image-model");

internal sealed class SmokeTransport : IOpenRouterTransport
{
    public Task<OpenRouterTransportResponse> SendAsync(
        OpenRouterTransportRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Body.IndexOf("\"modalities\"") >= 0)
        {
            return Task.FromResult(new OpenRouterTransportResponse(
                200,
                "{\"choices\":[{\"message\":{\"images\":[{\"image_url\":{\"url\":\"data:image/png;base64,aGVsbG8=\"}}]}}]}"));
        }

        if (request.Uri.ToString().IndexOf("/completions") >= 0)
        {
            return Task.FromResult(new OpenRouterTransportResponse(
                200,
                "{\"choices\":[{\"text\":\"raw response\"}]}"));
        }

        return Task.FromResult(new OpenRouterTransportResponse(
            200,
            "{\"choices\":[{\"message\":{\"content\":\"chat response\"}}]}"));
    }
}

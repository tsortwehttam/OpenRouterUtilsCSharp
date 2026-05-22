# OpenRouterUtils

A small C# client for OpenRouter text, chat, raw completion, and image-generation calls.

The library is designed for game and app runtimes that care about AOT compatibility:

- targets `netstandard2.1` for Unity/MonoGame style consumers and `net8.0` for modern .NET
- avoids reflection-based JSON serialization
- uses an injectable HTTP transport so Unity, consoles, or custom engine networking can provide their own implementation
- keeps the public API friendly for the common cases

## Install

Reference `src/OpenRouterUtils/OpenRouterUtils.csproj` from your app, or pack it with:

```bash
dotnet pack src/OpenRouterUtils/OpenRouterUtils.csproj -c Release
```

## Quick Start

```csharp
using OpenRouterUtils;

var client = new OpenRouterClient("sk-or-v1-...");

var text = await client.CompleteTextAsync(
    "Write a one-sentence tavern rumor.",
    "openai/gpt-4.1-mini");

var reply = await client.ChatTextAsync(
    new[]
    {
        ChatMessage.System("Answer like a concise game NPC."),
        ChatMessage.User("Where is the blacksmith?")
    },
    "openai/gpt-4.1-mini");
```

## Model Fallback

Pass multiple models when you want OpenRouterUtils to retry unavailable model errors:

```csharp
var reply = await client.ChatTextAsync(
    new[] { ChatMessage.User("Give me a crafting hint.") },
    new[] { "openai/gpt-4.1-mini", "openai/gpt-4o-mini" });
```

## Conversations

```csharp
var npc = client.CreateConversation(
    new[] { "openai/gpt-4.1-mini" },
    ChatMessage.System("You are a terse sci-fi mechanic."));

var a = await npc.SendAsync("Can you fix my ship?");
var b = await npc.SendAsync("How much will it cost?");
```

`OpenRouterConversation` stores assistant replies in memory. You can inspect or persist `npc.Messages`.

## Raw Completion

Use `CompleteRawAsync` for non-chat/base completion models:

```csharp
var completion = await client.CompleteRawAsync(
    "The dungeon door opened and",
    new RawCompletionOptions { MaxTokens = 64, Stop = { "\n" } });
```

## Image Generation

```csharp
var image = await client.GenerateImageAsync(
    "A cozy pixel-art potion shop at night",
    "google/gemini-2.5-flash-image",
    new ImageGenerationOptions { AspectRatio = ImageAspectRatio.Wide16x9 });

byte[] pngBytes = image.GetBytes();
```

## Unity, Consoles, and Custom Transports

The default transport uses `HttpClient`. If that is unsuitable for your runtime, implement:

```csharp
public interface IOpenRouterTransport
{
    Task<OpenRouterTransportResponse> SendAsync(
        OpenRouterTransportRequest request,
        CancellationToken cancellationToken);
}
```

Then pass it into `OpenRouterClientOptions.Transport`.

```csharp
var client = new OpenRouterClient(new OpenRouterClientOptions("sk-or-v1-...")
{
    Transport = new MyUnityOrConsoleTransport(),
    DisposeTransport = true
});
```

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

## Browser / Blazor WebAssembly

OpenRouterUtils runs in Blazor WebAssembly without any extra package. The `net8.0`
build is `IsAotCompatible` and works under the browser's WASM runtime; no
filesystem, sockets, threads, or reflection-based JSON are used.

Reuse the DI-registered `HttpClient` (which routes through the browser `fetch`
API) by passing it into the existing transport:

```csharp
// Program.cs (Blazor WebAssembly)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped(sp =>
{
    var http = sp.GetRequiredService<HttpClient>();
    var transport = new HttpClientOpenRouterTransport(http, disposeClient: false);
    return new OpenRouterClient(new OpenRouterClientOptions("sk-or-v1-...")
    {
        Transport = transport,
        DisposeTransport = false
    });
});
```

### Browser caveats (read before shipping)

- **API keys leak.** Any key bundled into a WASM app is visible to users. Run a
  server-side proxy in production and point `HttpClient.BaseAddress` at it
  instead of calling `openrouter.ai` directly.
- **CORS.** Direct browser calls to `openrouter.ai` require the origin to be
  allowed by OpenRouter. A proxy sidesteps this.
- **No sync I/O.** All APIs here are already async, so this is fine — but do
  not call `.Result` / `.Wait()` on the main browser thread.
- **Restricted headers.** The browser blocks some headers (e.g. `User-Agent`,
  `Referer`). If you customize headers, expect them to be ignored.

These caveats do not apply to non-browser targets (Unity, iOS, consoles,
macOS/Windows/Linux desktop, server) which keep using `HttpClient` or a custom
`IOpenRouterTransport` exactly as before.

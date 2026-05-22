using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenRouterUtils.Internal;

namespace OpenRouterUtils;

public sealed class OpenRouterClient : IDisposable
{
    private readonly OpenRouterClientOptions _options;
    private readonly IOpenRouterTransport _transport;
    private readonly bool _disposeTransport;

    public OpenRouterClient(string apiKey)
        : this(new OpenRouterClientOptions(apiKey))
    {
    }

    public OpenRouterClient(OpenRouterClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _transport = options.Transport ?? new HttpClientOpenRouterTransport();
        _disposeTransport = options.Transport == null || options.DisposeTransport;
    }

    public Task<string> CompleteTextAsync(
        string prompt,
        string model,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return CompleteTextAsync(prompt, ModelList.One(model), options, cancellationToken);
    }

    public async Task<string> CompleteTextAsync(
        string prompt,
        IEnumerable<string>? models = null,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await ChatAsync(
            new[] { ChatMessage.User(prompt) },
            ModelList.CopyOrDefault(models, _options.DefaultChatModels),
            ToChatOptions(options),
            cancellationToken).ConfigureAwait(false);
        return result.Text;
    }

    public Task<string> ChatTextAsync(
        IEnumerable<ChatMessage> messages,
        string model,
        ChatRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return ChatTextAsync(messages, ModelList.One(model), options, cancellationToken);
    }

    public async Task<string> ChatTextAsync(
        IEnumerable<ChatMessage> messages,
        IEnumerable<string>? models = null,
        ChatRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await ChatAsync(messages, models, options, cancellationToken).ConfigureAwait(false);
        return result.Text;
    }

    public Task<OpenRouterTextResult> ChatAsync(
        IEnumerable<ChatMessage> messages,
        string model,
        ChatRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return ChatAsync(messages, ModelList.One(model), options, cancellationToken);
    }

    public Task<OpenRouterTextResult> ChatAsync(
        IEnumerable<ChatMessage> messages,
        IEnumerable<string>? models = null,
        ChatRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var modelList = ModelList.CopyOrDefault(models, _options.DefaultChatModels);
        var messageList = CopyMessages(messages);
        return ModelFallback.RunAsync(
            modelList,
            (model, ct) => SendChatAsync(messageList, model, options ?? new ChatRequestOptions(), ct),
            cancellationToken);
    }

    public Task<string> CompleteRawAsync(
        string prompt,
        RawCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return CompleteRawAsync(prompt, _options.DefaultRawCompletionModels, options, cancellationToken);
    }

    public Task<string> CompleteRawAsync(
        string prompt,
        string model,
        RawCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return CompleteRawAsync(prompt, ModelList.One(model), options, cancellationToken);
    }

    public async Task<string> CompleteRawAsync(
        string prompt,
        IEnumerable<string> models,
        RawCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await ModelFallback.RunAsync(
            ModelList.CopyOrDefault(models, _options.DefaultRawCompletionModels),
            (model, ct) => SendRawCompletionAsync(prompt, model, options ?? new RawCompletionOptions(), ct),
            cancellationToken).ConfigureAwait(false);
        return result.Text;
    }

    public Task<GeneratedImage> GenerateImageAsync(
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return GenerateImageAsync(prompt, _options.DefaultImageModels, options, cancellationToken);
    }

    public Task<GeneratedImage> GenerateImageAsync(
        string prompt,
        string model,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return ModelFallback.RunAsync(
            ModelList.One(model),
            (activeModel, ct) => SendImageAsync(prompt, activeModel, options ?? new ImageGenerationOptions(), ct),
            cancellationToken);
    }

    public Task<GeneratedImage> GenerateImageAsync(
        string prompt,
        IEnumerable<string> models,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return ModelFallback.RunAsync(
            ModelList.CopyOrDefault(models, _options.DefaultImageModels),
            (activeModel, ct) => SendImageAsync(prompt, activeModel, options ?? new ImageGenerationOptions(), ct),
            cancellationToken);
    }

    public OpenRouterConversation CreateConversation(
        string model,
        ChatMessage? systemMessage = null,
        ChatRequestOptions? options = null)
    {
        return new OpenRouterConversation(this, ModelList.One(model), systemMessage, options);
    }

    public OpenRouterConversation CreateConversation(
        IEnumerable<string>? models = null,
        ChatMessage? systemMessage = null,
        ChatRequestOptions? options = null)
    {
        return new OpenRouterConversation(
            this,
            ModelList.CopyOrDefault(models, _options.DefaultChatModels),
            systemMessage,
            options);
    }

    public void Dispose()
    {
        if (_disposeTransport && _transport is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private async Task<OpenRouterTextResult> SendChatAsync(
        IEnumerable<ChatMessage> messages,
        string model,
        ChatRequestOptions options,
        CancellationToken cancellationToken)
    {
        var body = Json.Write(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("model", model);
            WriteMessages(writer, messages);
            WriteCommonOptions(writer, options.MaxTokens, options.Temperature, options.TopP, options.Seed, options.Stop);
            WriteResponseFormat(writer, options.ResponseFormat);
            writer.WriteEndObject();
        });

        var responseBody = await SendJsonAsync("chat/completions", body, cancellationToken).ConfigureAwait(false);
        return ParseTextResult(responseBody);
    }

    private async Task<OpenRouterTextResult> SendRawCompletionAsync(
        string prompt,
        string model,
        RawCompletionOptions options,
        CancellationToken cancellationToken)
    {
        var body = Json.Write(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("model", model);
            writer.WriteString("prompt", prompt ?? string.Empty);
            WriteCommonOptions(writer, options.MaxTokens, options.Temperature, options.TopP, options.Seed, options.Stop);
            writer.WriteEndObject();
        });

        var responseBody = await SendJsonAsync("completions", body, cancellationToken).ConfigureAwait(false);
        return ParseTextResult(responseBody);
    }

    private async Task<GeneratedImage> SendImageAsync(
        string prompt,
        string model,
        ImageGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var body = Json.Write(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("model", model);
            writer.WritePropertyName("messages");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("role", "user");
            writer.WriteString("content", string.Concat(prompt ?? string.Empty, "\n\nAspect ratio: ", ImageAspectRatioText(options.AspectRatio)));
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WritePropertyName("modalities");
            writer.WriteStartArray();
            writer.WriteStringValue("image");
            writer.WriteStringValue("text");
            writer.WriteEndArray();
            if (options.Seed.HasValue)
            {
                writer.WriteNumber("seed", options.Seed.Value);
            }

            writer.WriteEndObject();
        });

        var responseBody = await SendJsonAsync("chat/completions", body, cancellationToken).ConfigureAwait(false);
        return ParseImage(responseBody);
    }

    private async Task<string> SendJsonAsync(string relativePath, string body, CancellationToken cancellationToken)
    {
        var uri = new Uri(EnsureTrailingSlash(_options.BaseUri), relativePath);
        var headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer " + _options.ApiKey },
            { "Content-Type", "application/json" }
        };
        if (!string.IsNullOrWhiteSpace(_options.Referer))
        {
            headers.Add("HTTP-Referer", _options.Referer!);
        }

        if (!string.IsNullOrWhiteSpace(_options.Title))
        {
            headers.Add("X-Title", _options.Title!);
        }

        var response = await _transport.SendAsync(
            new OpenRouterTransportRequest("POST", uri, headers, body),
            cancellationToken).ConfigureAwait(false);

        if (response.StatusCode < 200 || response.StatusCode >= 300)
        {
            throw new OpenRouterException(
                response.StatusCode,
                "OpenRouter returned HTTP " + response.StatusCode + ".",
                response.Body);
        }

        return response.Body;
    }

    private static OpenRouterTextResult ParseTextResult(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        var id = JsonRead.StringProperty(root, "id");
        var model = JsonRead.StringProperty(root, "model");
        OpenRouterUsage? usage = null;

        if (root.TryGetProperty("usage", out var usageElement))
        {
            usage = new OpenRouterUsage(
                JsonRead.IntProperty(usageElement, "prompt_tokens"),
                JsonRead.IntProperty(usageElement, "completion_tokens"),
                JsonRead.IntProperty(usageElement, "total_tokens"));
        }

        var text = string.Empty;
        var finishReason = default(string);
        if (root.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array)
        {
            foreach (var choice in choices.EnumerateArray())
            {
                finishReason ??= JsonRead.StringProperty(choice, "finish_reason");
                if (choice.TryGetProperty("message", out var message))
                {
                    text += JsonRead.StringProperty(message, "content") ?? string.Empty;
                }
                else
                {
                    text += JsonRead.StringProperty(choice, "text") ?? string.Empty;
                }
            }
        }

        return new OpenRouterTextResult(text, model, id, finishReason, usage);
    }

    private static GeneratedImage ParseImage(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;

        if (root.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array)
        {
            foreach (var choice in choices.EnumerateArray())
            {
                if (!choice.TryGetProperty("message", out var message) ||
                    !message.TryGetProperty("images", out var images) ||
                    images.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var image in images.EnumerateArray())
                {
                    if (!image.TryGetProperty("image_url", out var imageUrl))
                    {
                        continue;
                    }

                    var dataUrl = JsonRead.StringProperty(imageUrl, "url");
                    if (TryParseImageDataUrl(dataUrl, out var parsed))
                    {
                        return parsed!;
                    }
                }
            }
        }

        throw new OpenRouterException(200, "OpenRouter response did not contain a generated image.", responseBody);
    }

    private static bool TryParseImageDataUrl(string? dataUrl, out GeneratedImage? image)
    {
        image = null;
        if (string.IsNullOrWhiteSpace(dataUrl) ||
            !dataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var marker = ";base64,";
        var markerIndex = dataUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return false;
        }

        var mimeType = dataUrl.Substring("data:".Length, markerIndex - "data:".Length);
        var base64 = dataUrl.Substring(markerIndex + marker.Length);
        image = new GeneratedImage(dataUrl, mimeType, base64);
        return true;
    }

    private static void WriteMessages(Utf8JsonWriter writer, IEnumerable<ChatMessage> messages)
    {
        writer.WritePropertyName("messages");
        writer.WriteStartArray();
        var wroteAny = false;
        foreach (var message in messages)
        {
            wroteAny = true;
            writer.WriteStartObject();
            writer.WriteString("role", RoleText(message.Role));
            writer.WriteString("content", message.Content);
            writer.WriteEndObject();
        }

        if (!wroteAny)
        {
            writer.WriteStartObject();
            writer.WriteString("role", "user");
            writer.WriteString("content", string.Empty);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteCommonOptions(
        Utf8JsonWriter writer,
        int? maxTokens,
        double? temperature,
        double? topP,
        int? seed,
        IList<string> stop)
    {
        if (maxTokens.HasValue)
        {
            writer.WriteNumber("max_tokens", maxTokens.Value);
        }

        if (temperature.HasValue)
        {
            writer.WriteNumber("temperature", temperature.Value);
        }

        if (topP.HasValue)
        {
            writer.WriteNumber("top_p", topP.Value);
        }

        if (seed.HasValue)
        {
            writer.WriteNumber("seed", seed.Value);
        }

        if (stop.Count > 0)
        {
            writer.WritePropertyName("stop");
            writer.WriteStartArray();
            foreach (var value in stop)
            {
                writer.WriteStringValue(value);
            }

            writer.WriteEndArray();
        }
    }

    private static void WriteResponseFormat(Utf8JsonWriter writer, OpenRouterResponseFormat? responseFormat)
    {
        if (responseFormat == null)
        {
            return;
        }

        writer.WritePropertyName("response_format");
        writer.WriteStartObject();
        writer.WriteString("type", responseFormat.Type);
        if (responseFormat.Type == "json_schema")
        {
            writer.WritePropertyName("json_schema");
            writer.WriteStartObject();
            writer.WriteString("name", responseFormat.SchemaName ?? "response_schema");
            writer.WritePropertyName("schema");
            using (var schema = JsonDocument.Parse(responseFormat.SchemaJson ?? "{}"))
            {
                schema.RootElement.WriteTo(writer);
            }

            writer.WriteBoolean("strict", responseFormat.Strict);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static ChatRequestOptions ToChatOptions(TextCompletionOptions? options)
    {
        if (options == null)
        {
            return new ChatRequestOptions();
        }

        var chat = new ChatRequestOptions
        {
            MaxTokens = options.MaxTokens,
            Temperature = options.Temperature,
            TopP = options.TopP,
            Seed = options.Seed
        };

        foreach (var stop in options.Stop)
        {
            chat.Stop.Add(stop);
        }

        return chat;
    }

    private static IReadOnlyList<ChatMessage> CopyMessages(IEnumerable<ChatMessage> messages)
    {
        var list = new List<ChatMessage>();
        foreach (var message in messages)
        {
            list.Add(message);
        }

        if (list.Count == 0)
        {
            list.Add(ChatMessage.User(string.Empty));
        }

        return list;
    }

    private static string RoleText(ChatMessageRole role)
    {
        return role switch
        {
            ChatMessageRole.Assistant => "assistant",
            ChatMessageRole.Developer => "system",
            ChatMessageRole.System => "system",
            _ => "user"
        };
    }

    private static string ImageAspectRatioText(ImageAspectRatio aspectRatio)
    {
        return aspectRatio switch
        {
            ImageAspectRatio.Portrait2x3 => "2:3",
            ImageAspectRatio.Landscape3x2 => "3:2",
            ImageAspectRatio.Portrait3x4 => "3:4",
            ImageAspectRatio.Landscape4x3 => "4:3",
            ImageAspectRatio.Portrait4x5 => "4:5",
            ImageAspectRatio.Landscape5x4 => "5:4",
            ImageAspectRatio.Portrait9x16 => "9:16",
            ImageAspectRatio.Wide16x9 => "16:9",
            ImageAspectRatio.UltraWide21x9 => "21:9",
            ImageAspectRatio.Tall1x4 => "1:4",
            ImageAspectRatio.Banner8x1 => "8:1",
            _ => "1:1"
        };
    }

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        var value = uri.ToString();
        return value.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri(value + "/");
    }
}

namespace OpenRouterUtils;

public sealed class ImageGenerationOptions
{
    public ImageAspectRatio AspectRatio { get; set; } = ImageAspectRatio.Square1x1;

    public int? Seed { get; set; }
}

using System;

namespace OpenRouterUtils;

public sealed class GeneratedImage
{
    public GeneratedImage(string dataUrl, string mimeType, string base64)
    {
        DataUrl = dataUrl;
        MimeType = mimeType;
        Base64 = base64;
    }

    public string DataUrl { get; }

    public string MimeType { get; }

    public string Base64 { get; }

    public byte[] GetBytes()
    {
        return Convert.FromBase64String(Base64);
    }
}

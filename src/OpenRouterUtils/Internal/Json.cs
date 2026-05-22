using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace OpenRouterUtils.Internal;

internal static class Json
{
    public static string Write(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            write(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

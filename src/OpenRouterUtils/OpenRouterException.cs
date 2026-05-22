using System;

namespace OpenRouterUtils;

public sealed class OpenRouterException : Exception
{
    public OpenRouterException(int statusCode, string message, string responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public int StatusCode { get; }

    public string ResponseBody { get; }
}

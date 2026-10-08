namespace Remitter.Client;

public sealed class RemitterApiException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

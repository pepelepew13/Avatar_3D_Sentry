using System;

namespace AvatarSentry.Application.Exceptions;

public class AvatarSentryException : Exception
{
    public AvatarSentryException(string message, int statusCode = 500, string? details = null) : base(message)
    {
        StatusCode = statusCode;
        Details = details;
    }

    public int StatusCode { get; }
    public string? Details { get; }
}

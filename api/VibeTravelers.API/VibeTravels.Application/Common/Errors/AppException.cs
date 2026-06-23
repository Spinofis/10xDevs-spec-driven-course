using System.Net;

namespace VibeTravels.Application.Common.Errors;

public abstract class AppException : Exception
{
    public string ErrorCode { get; }
    public HttpStatusCode StatusCode { get; }

    protected AppException(string message, string errorCode, HttpStatusCode statusCode)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}


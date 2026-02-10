using System.Net;

namespace VibeTravels.Application.Common.Errors;

public sealed class EmailTakenException : AppException
{
    public const string ErrorCode = "EMAIL_TAKEN";

    public EmailTakenException()
        : base("Email is already registered.", ErrorCode, HttpStatusCode.Conflict)
    {
    }
}

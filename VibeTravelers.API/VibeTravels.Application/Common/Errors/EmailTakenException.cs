namespace VibeTravels.Application.Common.Errors;

public sealed class EmailTakenException : Exception
{
    public const string ErrorCode = "EMAIL_TAKEN";

    public EmailTakenException() : base("Email is already registered.") { }
}

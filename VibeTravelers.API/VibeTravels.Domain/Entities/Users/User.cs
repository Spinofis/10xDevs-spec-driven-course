using VibeTravels.Domain.ValueObjects;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Domain.Entities.Users;

public sealed class User
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; } = null!;
    public Password PasswordHash { get; private set; } = null!;
    public string? DisplayName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private User() { }

    public static Result<User> Create(string? email, string? passwordHash)
    {
        var emailResult = Email.Create(email);
        if (emailResult.IsSuccess is false || emailResult.Value is null)
            return Result<User>.Fail(emailResult.Errors);

        var passwordResult = Password.Create(passwordHash);
        if (passwordResult.IsSuccess is false || passwordResult.Value is null)
            return Result<User>.Fail(passwordResult.Errors);

        return Result<User>.Ok(Create(emailResult.Value, passwordResult.Value));
    }

    private static User Create(Email email, Password passwordHash)
    {
        var now = DateTime.UtcNow;
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Common.Errors;
using VibeTravels.Domain.Entities.Users;

namespace VibeTravels.Application.Features.Auth.Commands;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserCommandResponse>
{
    private readonly IAppDbContext _db;

    public RegisterUserCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<RegisterUserCommandResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (exists)
            throw new EmailTakenException();

        var user = User.Create(email, request.Request.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return new RegisterUserCommandResponse();
    }
}

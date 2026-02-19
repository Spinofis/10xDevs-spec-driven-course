using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Common.Results;
using VibeTravels.Application.Features.Auth.Commands;
using VibeTravels.Domain.Entities.Users;

namespace VibeTravels.Application.Features.Auth.Handlers;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result>
{
    private readonly IAppDbContext _db;

    public RegisterUserCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (exists)
            return Result.Fail(ResultErrors.EmailTaken(nameof(request.Request.Email)));

        var user = User.Create(email, request.Request.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}

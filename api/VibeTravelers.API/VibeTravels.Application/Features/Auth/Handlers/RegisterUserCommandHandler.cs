using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Auth.Commands;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.Common.Results;

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
        var createUserResult = User.Create(request.Request.Email, request.Request.Password);
        if (createUserResult.IsSuccess is false || createUserResult.Value is null)
            return Result.Fail(createUserResult.Errors);

        var user = createUserResult.Value;

        var exists = await _db.Users
            .AnyAsync(u => u.Email == user.Email, cancellationToken);

        if (exists)
            return Result.Fail(ResultErrors.EmailTaken(nameof(request.Request.Email)));

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}

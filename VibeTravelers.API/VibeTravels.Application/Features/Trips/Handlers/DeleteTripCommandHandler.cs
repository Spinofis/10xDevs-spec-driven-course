using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Trips.Commands;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Trips.Handlers;

public sealed class DeleteTripCommandHandler : IRequestHandler<DeleteTripCommand, Result>
{
    private readonly IAppDbContext _db;

    public DeleteTripCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeleteTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _db.Trips.SingleOrDefaultAsync(
            t => t.Id == request.Request.TripId
                 && t.UserId == request.UserId
                 && t.DeletedAt == null,
            cancellationToken);

        if (trip is null)
            return Result.Fail(ResultErrors.TripNotFound(nameof(request.Request.TripId)));

        var deleteResult = trip.SoftDelete(DateTimeOffset.UtcNow);
        if (deleteResult.IsSuccess is false)
            return Result.Fail(deleteResult.Errors);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}

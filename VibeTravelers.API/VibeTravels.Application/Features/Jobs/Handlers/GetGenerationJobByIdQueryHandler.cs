using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Jobs.Queries;
using VibeTravels.Application.Features.Jobs.Queries.Models;
using VibeTravels.Application.Features.Jobs.Services;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Jobs.Handlers;

public sealed class GetGenerationJobByIdQueryHandler : IRequestHandler<GetGenerationJobByIdQuery, Result<GetGenerationJobByIdQueryResponse>>
{
    private readonly IAppDbContext _db;
    private readonly IGenerationJobStatusMapper _generationJobStatusMapper;

    public GetGenerationJobByIdQueryHandler(
        IAppDbContext db,
        IGenerationJobStatusMapper generationJobStatusMapper)
    {
        _db = db;
        _generationJobStatusMapper = generationJobStatusMapper;
    }

    public async Task<Result<GetGenerationJobByIdQueryResponse>> Handle(
        GetGenerationJobByIdQuery request,
        CancellationToken cancellationToken)
    {
        var row = await _db.AiGenerationJobs
            .AsNoTracking()
            .Where(x => x.Id == request.Request.JobId && x.UserId == request.UserId)
            .Select(x => new
            {
                x.Id,
                x.TripId,
                x.Status,
                x.RequestedAt,
                x.StartedAt,
                x.FinishedAt,
                x.AttemptNo,
                x.ErrorCode,
                x.ErrorMessage,
                x.Discarded,
                x.DiscardReason
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
            return Result<GetGenerationJobByIdQueryResponse>.Fail(ResultErrors.JobNotFound(nameof(request.Request.JobId)));

        var job = new GenerationJobQueryModel(
            row.Id,
            row.TripId,
            _generationJobStatusMapper.Map(row.Status),
            row.RequestedAt,
            row.StartedAt,
            row.FinishedAt,
            row.AttemptNo,
            row.ErrorCode,
            row.ErrorMessage,
            row.Discarded,
            row.DiscardReason);

        return Result<GetGenerationJobByIdQueryResponse>.Ok(new GetGenerationJobByIdQueryResponse(job));
    }
}

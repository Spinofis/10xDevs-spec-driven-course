using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Plans.Commands;

public sealed record SavePlanCommand(Guid UserId, SavePlanCommandRequest Request)
    : IRequest<Result<SavePlanCommandResponse>>;

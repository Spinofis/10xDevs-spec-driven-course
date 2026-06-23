using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Plans.Commands;

public sealed record UpdatePlanCommand(Guid UserId, UpdatePlanCommandRequest Request)
    : IRequest<Result<UpdatePlanCommandResponse>>;

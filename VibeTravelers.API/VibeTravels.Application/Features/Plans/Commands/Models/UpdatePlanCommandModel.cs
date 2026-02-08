namespace VibeTravels.Application.Features.Plans.Commands.Models;

public sealed record UpdatePlanCommandModel(
    string? Summary,
    IReadOnlyList<PlanItemCommandModel> Items);

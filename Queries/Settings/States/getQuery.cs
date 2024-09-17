using MediatR;

namespace Klacks_api.Queries.Settings.States;

public record GetQuery(Guid Id) : IRequest<Models.Settings.State>;

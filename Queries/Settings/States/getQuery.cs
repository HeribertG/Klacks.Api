using MediatR;

namespace Klacks.Api.Queries.Settings.States;

public record GetQuery(Guid Id) : IRequest<Models.Settings.State>;

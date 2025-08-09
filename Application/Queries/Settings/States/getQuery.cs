using MediatR;

namespace Klacks.Api.Application.Queries.Settings.States;

public record GetQuery(Guid Id) : IRequest<Models.Settings.State>;

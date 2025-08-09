using MediatR;

namespace Klacks.Api.Application.Queries.Settings.States;

public record ListQuery : IRequest<IEnumerable<Models.Settings.State>>;

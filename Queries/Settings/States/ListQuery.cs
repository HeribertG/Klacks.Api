using MediatR;

namespace Klacks_api.Queries.Settings.States;

public record ListQuery : IRequest<IEnumerable<Models.Settings.State>>;

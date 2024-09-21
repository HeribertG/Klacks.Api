using MediatR;

namespace Klacks.Api.Queries.Settings.States;

public record ListQuery : IRequest<IEnumerable<Models.Settings.State>>;

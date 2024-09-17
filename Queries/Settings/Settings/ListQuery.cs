using MediatR;

namespace Klacks_api.Queries.Settings.Settings;

public record ListQuery : IRequest<IEnumerable<Models.Settings.Settings>>;

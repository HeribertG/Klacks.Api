using MediatR;

namespace Klacks.Api.Queries.Settings.Settings;

public record ListQuery : IRequest<IEnumerable<Models.Settings.Settings>>;

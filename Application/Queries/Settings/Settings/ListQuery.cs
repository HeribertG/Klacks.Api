using MediatR;

namespace Klacks.Api.Application.Queries.Settings.Settings;

public record ListQuery : IRequest<IEnumerable<Models.Settings.Settings>>;

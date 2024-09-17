using MediatR;

namespace Klacks_api.Queries.Settings.Settings;

public record GetQuery(string Type) : IRequest<Models.Settings.Settings?>;

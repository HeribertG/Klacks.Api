using MediatR;

namespace Klacks.Api.Queries.Settings.Settings;

public record GetQuery(string Type) : IRequest<Models.Settings.Settings?>;

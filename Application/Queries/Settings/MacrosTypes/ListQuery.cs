using MediatR;

namespace Klacks.Api.Queries.Settings.MacrosTypes;

public record ListQuery : IRequest<IEnumerable<Models.Settings.MacroType>>;

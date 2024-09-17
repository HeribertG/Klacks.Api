using MediatR;

namespace Klacks_api.Queries.Settings.MacrosTypes;

public record ListQuery : IRequest<IEnumerable<Models.Settings.MacroType>>;

using MediatR;

namespace Klacks_api.Queries.Settings.MacrosTypes;

public record GetQuery(Guid Id) : IRequest<Models.Settings.MacroType>;

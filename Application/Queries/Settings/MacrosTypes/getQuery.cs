using MediatR;

namespace Klacks.Api.Queries.Settings.MacrosTypes;

public record GetQuery(Guid Id) : IRequest<Models.Settings.MacroType>;

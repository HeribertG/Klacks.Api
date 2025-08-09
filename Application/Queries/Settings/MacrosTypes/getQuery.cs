using MediatR;

namespace Klacks.Api.Application.Queries.Settings.MacrosTypes;

public record GetQuery(Guid Id) : IRequest<Models.Settings.MacroType>;

using MediatR;

namespace Klacks.Api.Application.Queries.Settings.MacrosTypes;

public record ListQuery : IRequest<IEnumerable<Models.Settings.MacroType>>;

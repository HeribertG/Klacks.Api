using Klacks_api.Resources.Settings;
using MediatR;

namespace Klacks_api.Queries.Settings.Macros;

public record GetQuery(Guid Id) : IRequest<MacroResource>;

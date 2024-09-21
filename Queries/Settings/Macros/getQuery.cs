using Klacks.Api.Resources.Settings;
using MediatR;

namespace Klacks.Api.Queries.Settings.Macros;

public record GetQuery(Guid Id) : IRequest<MacroResource>;

using Klacks.Api.Resources.Settings;
using MediatR;

namespace Klacks.Api.Queries.Settings.Macros;

public record ListQuery : IRequest<IEnumerable<MacroResource>>;

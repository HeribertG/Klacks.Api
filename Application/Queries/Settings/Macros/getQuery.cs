using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Queries.Settings.Macros;

public record GetQuery(Guid Id) : IRequest<MacroResource>;

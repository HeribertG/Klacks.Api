// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public sealed record SpeechModelCheckResponse(IReadOnlyList<SpeechModelCheckDto> Models);

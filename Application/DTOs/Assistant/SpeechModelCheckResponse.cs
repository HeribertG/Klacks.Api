// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public sealed record SpeechModelCheckResponse(IReadOnlyList<SpeechModelCheckDto> Models);

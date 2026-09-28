// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Assistant;

public class EscalationAcknowledgeResultResource
{
    [JsonConverter(typeof(JsonStringEnumConverter<EscalationAcknowledgeOutcome>))]
    public EscalationAcknowledgeOutcome Outcome { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<EscalationChainStatus>))]
    public EscalationChainStatus? ChainStatus { get; set; }
}

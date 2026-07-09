// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SlotMatchMode
{
    [JsonStringEnumMemberName("exact")]
    Exact,

    [JsonStringEnumMemberName("contains")]
    Contains,

    [JsonStringEnumMemberName("ignore")]
    Ignore,

    [JsonStringEnumMemberName("resolved-entity-id")]
    ResolvedEntityId
}

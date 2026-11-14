// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// See ITurnToolsetExtension. Holds the offers of one turn only; an offer for another turn id starts over, so a scope
/// shared by several turns does not carry skills from one turn into the next. Turns without a turn id (eval replay,
/// probes) share the null key; none of them runs a tool loop that calls find_tools.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.ToolSearch;

public sealed class TurnToolsetExtension : ITurnToolsetExtension
{
    private readonly object _gate = new();
    private readonly List<LLMFunction> _offered = [];
    private readonly HashSet<string> _offeredNames = new(StringComparer.OrdinalIgnoreCase);
    private Guid? _turnId;
    private int _takenCount;

    public IReadOnlyList<LLMFunction> Offer(Guid? turnId, IReadOnlyList<LLMFunction> functions)
    {
        lock (_gate)
        {
            if (_turnId != turnId)
            {
                _turnId = turnId;
                _offered.Clear();
                _offeredNames.Clear();
                _takenCount = 0;
            }

            var accepted = new List<LLMFunction>();
            foreach (var function in functions)
            {
                if (_offeredNames.Count >= ToolSearchConstants.MaxAddedToolsPerTurn)
                {
                    break;
                }

                if (_offeredNames.Add(function.Name))
                {
                    _offered.Add(function);
                    accepted.Add(function);
                }
            }

            return accepted;
        }
    }

    public IReadOnlyList<LLMFunction> TakeOffered(Guid? turnId)
    {
        lock (_gate)
        {
            if (_turnId != turnId || _takenCount == _offered.Count)
            {
                return [];
            }

            var pending = _offered.Skip(_takenCount).ToList();
            _takenCount = _offered.Count;
            return pending;
        }
    }
}

// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// The one installation-wide switch of Klacksy's explain offers, shared by the gate offers (T4) and the UI offers (T5):
/// on while the setting KLACKSY_EXPLAIN_OFFERS_ENABLED is not "false" and the proactive kill switch is off.
/// </summary>
public interface IExplainOffersSwitch
{
    Task<bool> IsOnAsync(CancellationToken cancellationToken = default);
}

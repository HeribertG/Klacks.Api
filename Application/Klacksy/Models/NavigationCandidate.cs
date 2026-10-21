// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Klacksy.Models;

public sealed record NavigationCandidate(string TargetId, string Route, double Score);

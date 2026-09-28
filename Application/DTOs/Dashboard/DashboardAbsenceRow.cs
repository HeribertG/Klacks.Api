// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Dashboard;

public record DashboardAbsenceRow(DateTime From, DateTime Until, double DefaultValue);

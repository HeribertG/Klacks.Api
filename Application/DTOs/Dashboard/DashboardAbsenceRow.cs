// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Dashboard;

public record DashboardAbsenceRow(DateTime From, DateTime Until, double DefaultValue);

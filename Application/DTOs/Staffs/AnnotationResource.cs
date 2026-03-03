// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Staffs;

public class AnnotationResource
{
    public Guid ClientId { get; set; }

    public Guid Id { get; set; }

    public string Note { get; set; } = string.Empty;
}

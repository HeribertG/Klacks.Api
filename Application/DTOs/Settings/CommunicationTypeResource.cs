// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Settings
{
    public class CommunicationTypeResource
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public int Category { get; set; }
        public int DefaultIndex { get; set; }
    }
}
// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Domain.Models.Settings
{
    public class Settings
    {
        [Key]
        public Guid Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}

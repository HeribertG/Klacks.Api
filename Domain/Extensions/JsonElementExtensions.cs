// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lookup helpers for JSON payloads whose property casing is not under our control. Skill results are
/// serialized by the skills themselves and by plugin code, so the same logical field arrives as
/// "customerId" or "CustomerId" depending on the producer.
/// </summary>

using System.Text.Json;

namespace Klacks.Api.Domain.Extensions
{
    public static class JsonElementExtensions
    {
        /// <summary>
        /// Finds a property regardless of its casing.
        /// </summary>
        /// <param name="element">Element to search; a non-object yields false</param>
        /// <param name="name">Property name to match case-insensitively</param>
        /// <param name="value">The matched property value, or default when nothing matched</param>
        public static bool TryGetPropertyCaseInsensitive(this JsonElement element, string name, out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }
    }
}

// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNumeric(this string s)
        {
            float output;
            return float.TryParse(s.Trim(), out output);
        }
    }
}

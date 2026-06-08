// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

public class PhoneticConfig
{
    public bool Enabled { get; set; } = true;
    public string Encoder { get; set; } = "rule_based";
    public int MinWordLength { get; set; } = 4;
    public int MaxEditDistance { get; set; } = 4;
    public List<PhoneticRule> Rules { get; set; } = [];
}

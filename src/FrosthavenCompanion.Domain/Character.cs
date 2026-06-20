namespace FrosthavenCompanion.Domain;

/// <summary>How a single perk checkbox was filled.</summary>
public enum PerkMark
{
    /// <summary>A normally-earned perk.</summary>
    Taken,

    /// <summary>An extra perk gained from the special bonus card — shown distinctly.</summary>
    Bonus,
}

/// <summary>
/// A party member: an optional name, the class played, and the current level
/// (1–9). Stored in <see cref="CampaignProgress.Party"/>; the levels feed the
/// recommended scenario level via <see cref="ScenarioLevelTable"/>.
/// </summary>
public sealed class Character
{
    public string Name { get; set; } = "";
    public string ClassName { get; set; } = "";
    public int Level { get; set; } = 1;

    /// <summary>
    /// Filled perk checkboxes, keyed by "{perkIndex}:{boxIndex}" against the
    /// class's perk list, with whether each was normally earned or from the
    /// bonus card. Absent keys are unchecked.
    /// </summary>
    public Dictionary<string, PerkMark> Perks { get; set; } = [];
}

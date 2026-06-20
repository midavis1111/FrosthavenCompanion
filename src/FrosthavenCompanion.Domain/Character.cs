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
/// A party member. The roster basics (name/class/level) drive the recommended
/// scenario level via <see cref="ScenarioLevelTable"/>; the rest mirrors the
/// physical character sheet (XP, gold, personal resources, notes, perks,
/// masteries) for the player's own "My Character" page.
/// </summary>
public sealed class Character
{
    /// <summary>Stable id so the player can mark which party member is "mine".</summary>
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";
    public string ClassName { get; set; } = "";
    public int Level { get; set; } = 1;

    public int Experience { get; set; }
    public int Gold { get; set; }
    public string Notes { get; set; } = "";

    /// <summary>
    /// Perks earned beyond leveling (battle goals, masteries, etc.) — the app
    /// can't derive these, so the player sets the count. Used by the perk-count
    /// validator together with the level.
    /// </summary>
    public int ExtraPerks { get; set; }

    /// <summary>Personal resource counts, keyed by resource slug (e.g. "lumber").</summary>
    public Dictionary<string, int> Resources { get; set; } = [];

    /// <summary>
    /// Filled perk checkboxes, keyed by "{perkIndex}:{boxIndex}" against the
    /// class's perk list, with whether each was normally earned or from the
    /// bonus card. Absent keys are unchecked.
    /// </summary>
    public Dictionary<string, PerkMark> Perks { get; set; } = [];

    /// <summary>Indices (into the class's mastery list) of completed masteries.</summary>
    public HashSet<int> Masteries { get; set; } = [];
}

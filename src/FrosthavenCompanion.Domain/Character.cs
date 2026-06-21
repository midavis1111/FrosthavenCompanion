namespace FrosthavenCompanion.Domain;

/// <summary>How a single perk checkbox was filled.</summary>
public enum PerkMark
{
    /// <summary>A normally-earned perk.</summary>
    Taken,

    /// <summary>An extra perk gained from the special bonus card — shown distinctly.</summary>
    Bonus,
}

/// <summary>Where an owned ability card currently sits.</summary>
public enum CardSlot
{
    /// <summary>Owned but not in the active scenario deck.</summary>
    Bench,

    /// <summary>In the active scenario deck.</summary>
    Deck,
}

/// <summary>Where a deck card sits during play. Hand is the default (no stored entry).</summary>
public enum PlayPile
{
    /// <summary>Currently in play (persistent/active area).</summary>
    Active,

    /// <summary>Played and discarded; recoverable on a rest.</summary>
    Discard,

    /// <summary>Lost (consumed); not recovered by a rest.</summary>
    Lost,
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

    /// <summary>Ability-card placement, keyed by card id (Deck or Bench); absent = unowned/unset.</summary>
    public Dictionary<int, CardSlot> Cards { get; set; } = [];

    /// <summary>During-play pile per deck card (Active/Discard/Lost); absent = in hand. Reset each scenario.</summary>
    public Dictionary<int, PlayPile> Play { get; set; } = [];
}

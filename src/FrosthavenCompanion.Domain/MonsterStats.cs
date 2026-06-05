namespace FrosthavenCompanion.Domain;

/// <summary>
/// A monster's stats for one level and rank (normal or elite). Values can be null
/// when the data doesn't specify them (e.g. range, which for most monsters lives on
/// their ability cards rather than the base stat card).
/// </summary>
public sealed record MonsterStatBlock
{
    // Strings rather than numbers because boss/summon stats can be formulas (e.g. "Cx20").
    public string? Health { get; init; }
    public string? Move { get; init; }
    public string? Attack { get; init; }
    public string? Range { get; init; }

    /// <summary>Special abilities printed on the stat card (e.g. "Shield 1", "Retaliate 2").</summary>
    public IReadOnlyList<string> Abilities { get; init; } = [];

    /// <summary>Condition names this monster inflicts (e.g. "poison"); shown with tooltips.</summary>
    public IReadOnlyList<string> Conditions { get; init; } = [];
}

/// <summary>Normal and elite stat blocks for a single scenario level (0–7).</summary>
public sealed record MonsterLevel
{
    public int Level { get; init; }
    public MonsterStatBlock? Normal { get; init; }
    public MonsterStatBlock? Elite { get; init; }
}

/// <summary>A monster and its stats across all scenario levels.</summary>
public sealed record MonsterStats
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }

    /// <summary>Conditions this monster is immune to.</summary>
    public IReadOnlyList<string> Immunities { get; init; } = [];

    public IReadOnlyList<MonsterLevel> Levels { get; init; } = [];
}

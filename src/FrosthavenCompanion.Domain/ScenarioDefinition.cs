namespace FrosthavenCompanion.Domain;

/// <summary>
/// A single node in the Frosthaven scenario flow chart. This is reference data
/// (the unlock graph), loaded from the embedded catalog and never modified at
/// runtime. Edges reference other scenarios by their <see cref="Index"/>.
/// </summary>
public sealed record ScenarioDefinition
{
    /// <summary>Scenario identifier as printed in the game, e.g. "1" or "4A".</summary>
    public required string Index { get; init; }

    public required string Name { get; init; }

    /// <summary>True if this scenario is available from the start of the campaign.</summary>
    public bool Initial { get; init; }

    /// <summary>Scenarios revealed when this one is completed.</summary>
    public IReadOnlyList<string> Unlocks { get; init; } = [];

    /// <summary>Scenarios permanently locked out when this one is completed (branch choices).</summary>
    public IReadOnlyList<string> Blocks { get; init; } = [];

    /// <summary>Scenarios that must be completed before this one can be played.</summary>
    public IReadOnlyList<string> Requires { get; init; } = [];

    /// <summary>Scenarios linked to this one (played in immediate succession).</summary>
    public IReadOnlyList<string> Links { get; init; } = [];

    /// <summary>Flow-chart grouping label, e.g. "intro".</summary>
    public string? Group { get; init; }

    /// <summary>Map grid location, when known.</summary>
    public string? Hex { get; init; }

    /// <summary>Flow-chart x coordinate (for a future visual layout).</summary>
    public double? X { get; init; }

    /// <summary>Flow-chart y coordinate (for a future visual layout).</summary>
    public double? Y { get; init; }
}

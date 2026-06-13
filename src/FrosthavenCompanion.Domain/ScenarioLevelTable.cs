namespace FrosthavenCompanion.Domain;

/// <summary>What a given scenario level yields, from the Frosthaven scenario level chart.</summary>
public sealed record ScenarioLevelInfo(
    int Level,
    int GoldConversion,
    int TrapDamage,
    int HazardousTerrain,
    int BonusExperience);

/// <summary>
/// The Frosthaven scenario level chart and the recommended-level calculation.
/// The scenario level sets monster stats, trap/hazardous-terrain damage, the gold
/// value of coins, and the end-of-scenario bonus experience.
/// </summary>
public static class ScenarioLevelTable
{
    public const int MinLevel = 0;
    public const int MaxLevel = 7;

    public static readonly IReadOnlyList<ScenarioLevelInfo> All =
    [
        new(0, 2, 2, 1, 4),
        new(1, 2, 3, 2, 6),
        new(2, 3, 4, 2, 8),
        new(3, 3, 5, 2, 10),
        new(4, 4, 6, 3, 12),
        new(5, 4, 7, 3, 14),
        new(6, 5, 8, 3, 16),
        new(7, 6, 9, 4, 18),
    ];

    /// <summary>The chart row for a level (clamped to 0–7).</summary>
    public static ScenarioLevelInfo Info(int level) => All[Math.Clamp(level, MinLevel, MaxLevel)];

    /// <summary>
    /// The recommended scenario level: the average character level divided by two,
    /// rounded up. Returns 1 for an empty party (Frosthaven's starting default).
    /// </summary>
    public static int Recommended(IReadOnlyCollection<int> characterLevels)
    {
        if (characterLevels.Count == 0) return 1;
        var average = characterLevels.Average();
        return (int)Math.Ceiling(average / 2.0);
    }

    /// <summary>The recommended level shifted by a difficulty modifier, clamped to 0–7.</summary>
    public static int Adjusted(IReadOnlyCollection<int> characterLevels, int difficultyModifier) =>
        Math.Clamp(Recommended(characterLevels) + difficultyModifier, MinLevel, MaxLevel);
}

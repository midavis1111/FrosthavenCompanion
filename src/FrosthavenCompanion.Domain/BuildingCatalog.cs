namespace FrosthavenCompanion.Domain;

/// <summary>The operating state of a built outpost building.</summary>
public enum BuildingCondition
{
    Operational,
    Damaged,
    Wrecked,
}

/// <summary>
/// The player's stored state for one building: its current upgrade level
/// (0 = not built) and operating condition. Part of <see cref="CampaignProgress"/>.
/// </summary>
public sealed class BuildingProgress
{
    public int Level { get; set; }
    public BuildingCondition Condition { get; set; } = BuildingCondition.Operational;
}

/// <summary>
/// Reference data for one Frosthaven outpost building: its construction number,
/// art slug, display name, and the highest upgrade level it can reach. Build
/// costs and effects are read off the building card art rather than modelled here.
/// </summary>
public sealed record Building(string Number, string Slug, string Name, int MaxLevel);

/// <summary>
/// The buildable/upgradeable Frosthaven outpost buildings (those with upgrade
/// cards), keyed by art slug. Ordered by construction number.
/// </summary>
public static class BuildingCatalog
{
    public static readonly IReadOnlyList<Building> All =
    [
        new("05", "mining-camp", "Mining Camp", 4),
        new("12", "hunting-lodge", "Hunting Lodge", 4),
        new("17", "logging-camp", "Logging Camp", 4),
        new("21", "inn", "Inn", 3),
        new("24", "garden", "Garden", 4),
        new("34", "craftsman", "Craftsman", 9),
        new("35", "alchemist", "Alchemist", 3),
        new("37", "trading-post", "Trading Post", 4),
        new("39", "jeweler", "Jeweler", 3),
        new("42", "temple-of-the-great-oak", "Temple of the Great Oak", 3),
        new("44", "enhancer", "Enhancer", 4),
        new("65", "metal-depot", "Metal Depot", 2),
        new("67", "lumber-depot", "Lumber Depot", 2),
        new("72", "hide-depot", "Hide Depot", 2),
        new("74", "tavern", "Tavern", 3),
        new("81", "hall-of-revelry", "Hall of Revelry", 2),
        new("83", "library", "Library", 3),
        new("84", "workshop", "Workshop", 1),
        new("85", "carpenter", "Carpenter", 2),
        new("88", "stables", "Stables", 4),
        new("90", "town-hall", "Town Hall", 3),
        new("98", "barracks", "Barracks", 4),
    ];

    public static Building? Find(string slug) =>
        All.FirstOrDefault(b => string.Equals(b.Slug, slug, StringComparison.OrdinalIgnoreCase));
}

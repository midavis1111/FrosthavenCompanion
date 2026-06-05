namespace FrosthavenCompanion.Domain;

/// <summary>One step of the outpost phase, grouped under its rulebook stage.</summary>
public sealed record OutpostStep(string Key, string Group, string Label);

/// <summary>
/// The ordered outpost-phase checklist — the rulebook stages (Passage of Time,
/// Outpost Event, Building Operations, Downtime, Prepare) broken into short
/// reminder steps. Authored labels, not rulebook text.
/// </summary>
public static class OutpostPhase
{
    public static readonly IReadOnlyList<OutpostStep> Steps =
    [
        new("pt-calendar", "Passage of Time", "Advance the calendar one week"),
        new("pt-section", "Passage of Time", "Read any section the new box lists"),

        new("oe-draw", "Outpost Event", "Draw & resolve an outpost event"),
        new("oe-cards", "Outpost Event", "Add or remove event cards as instructed"),

        new("bo-repair", "Building Operations", "Repair damaged buildings"),
        new("bo-build", "Building Operations", "Build or upgrade buildings"),
        new("bo-collect", "Building Operations", "Collect resources buildings produce"),

        new("dt-shop", "Downtime", "Buy, sell & craft items"),
        new("dt-enhance", "Downtime", "Enhance ability cards"),
        new("dt-level", "Downtime", "Level up & choose perks"),
        new("dt-manage", "Downtime", "Retire / manage characters & quests"),
        new("dt-donate", "Downtime", "Donate to the Sanctuary"),

        new("pr-next", "Prepare", "Choose & set up the next scenario"),
    ];

    /// <summary>The distinct stage names, in order.</summary>
    public static IReadOnlyList<string> Groups =>
        Steps.Select(s => s.Group).Distinct().ToList();
}

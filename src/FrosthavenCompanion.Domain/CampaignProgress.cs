namespace FrosthavenCompanion.Domain;

/// <summary>
/// The player's saved campaign state. Deliberately small: it records only what
/// cannot be derived — the party name and which scenarios have been completed
/// (with the date). Everything else (what is available, locked, hidden) is
/// computed by <see cref="CampaignEngine"/> from the catalog. This is the object
/// serialized to local storage and to export/import backups.
/// </summary>
public sealed class CampaignProgress
{
    public string PartyName { get; set; } = "New Party";

    /// <summary>
    /// When this save was last changed (UTC). Used to reconcile to the newest
    /// copy when syncing the same campaign across devices.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Completed scenario index -> date completed.</summary>
    public Dictionary<string, DateOnly> Completed { get; set; } = [];

    /// <summary>
    /// Scenarios unlocked by something outside the scenario graph (a class
    /// personal quest, town/outpost event, item, section book, ...), keyed by
    /// scenario index with an optional note describing the source. These are made
    /// available directly, since the engine cannot derive them.
    /// </summary>
    public Dictionary<string, string> ManualUnlocks { get; set; } = [];

    /// <summary>Optional per-scenario notes, keyed by scenario index.</summary>
    public Dictionary<string, string> Notes { get; set; } = [];

    /// <summary>
    /// The chosen play level per scenario (index → 0–7), remembered for the
    /// scenario-setup view. Scenarios with no entry fall back to
    /// <see cref="ScenarioLevel"/>.
    /// </summary>
    public Dictionary<string, int> ScenarioLevels { get; set; } = [];

    /// <summary>
    /// The campaign's default scenario level — the last level chosen anywhere,
    /// used to seed scenarios not yet in <see cref="ScenarioLevels"/>.
    /// </summary>
    public int ScenarioLevel { get; set; } = 1;

    /// <summary>How many times each scenario has been lost/retried, keyed by index.</summary>
    public Dictionary<string, int> ScenarioLosses { get; set; } = [];

    /// <summary>
    /// The campaign-wide outpost/town status (week, prosperity, resources, …).
    /// Defaults to a fresh status so saves written before this existed still load.
    /// </summary>
    public CampaignStatus Status { get; set; } = new();

    /// <summary>The party's characters (class + level), used to derive the scenario level.</summary>
    public List<Character> Party { get; set; } = [];

    /// <summary>Id of the player's own character (which party member is "mine") for the My Character page.</summary>
    public string? MyCharacterId { get; set; }

    /// <summary>Difficulty offset from the recommended scenario level (−2…+3).</summary>
    public int DifficultyModifier { get; set; }

    /// <summary>Outpost buildings the party has interacted with, keyed by art slug.</summary>
    public Dictionary<string, BuildingProgress> Buildings { get; set; } = [];

    /// <summary>Keys of the outpost-phase checklist steps ticked this phase.</summary>
    public HashSet<string> OutpostChecklist { get; set; } = [];

    public bool IsCompleted(string index) => Completed.ContainsKey(index);
}

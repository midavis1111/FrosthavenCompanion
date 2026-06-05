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

    public bool IsCompleted(string index) => Completed.ContainsKey(index);
}

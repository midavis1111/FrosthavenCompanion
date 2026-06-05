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

    /// <summary>Completed scenario index -> date completed.</summary>
    public Dictionary<string, DateOnly> Completed { get; set; } = [];

    /// <summary>Optional per-scenario notes, keyed by scenario index.</summary>
    public Dictionary<string, string> Notes { get; set; } = [];

    public bool IsCompleted(string index) => Completed.ContainsKey(index);
}

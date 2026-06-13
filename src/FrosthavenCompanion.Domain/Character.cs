namespace FrosthavenCompanion.Domain;

/// <summary>
/// A party member: an optional name, the class played, and the current level
/// (1–9). Stored in <see cref="CampaignProgress.Party"/>; the levels feed the
/// recommended scenario level via <see cref="ScenarioLevelTable"/>.
/// </summary>
public sealed class Character
{
    public string Name { get; set; } = "";
    public string ClassName { get; set; } = "";
    public int Level { get; set; } = 1;
}

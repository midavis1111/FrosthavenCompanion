namespace FrosthavenCompanion.Domain;

/// <summary>
/// The campaign-wide outpost/town status: the handful of running totals tracked
/// on the Frosthaven campaign sheet between scenarios (week, prosperity, morale,
/// inspiration, defense, reputation) plus the town's shared resource stockpile.
/// Like <see cref="CampaignProgress"/>, this is plain stored state the player
/// edits directly — nothing here is derived.
/// </summary>
public sealed class CampaignStatus
{
    /// <summary>Campaign calendar week. Frosthaven starts on week 1.</summary>
    public int Week { get; set; } = 1;

    /// <summary>Prosperity level (1–9), gating the item shop.</summary>
    public int Prosperity { get; set; } = 1;

    /// <summary>Checkmarks earned toward the next prosperity level.</summary>
    public int ProsperityTicks { get; set; }

    /// <summary>Outpost morale.</summary>
    public int Morale { get; set; }

    /// <summary>Unspent inspiration (spent to construct/upgrade buildings).</summary>
    public int Inspiration { get; set; }

    /// <summary>Outpost defense value, used when the town is attacked.</summary>
    public int Defense { get; set; }

    /// <summary>Town reputation (−20…+20), affecting shop prices.</summary>
    public int Reputation { get; set; }

    /// <summary>The town's shared resource supply, keyed by resource name.</summary>
    public Dictionary<string, int> Resources { get; set; } = [];

    /// <summary>
    /// The town resources tracked in the panel: the three crafting materials
    /// followed by the six herbs, in the order shown on the campaign sheet.
    /// </summary>
    public static readonly IReadOnlyList<string> ResourceNames =
    [
        "Lumber", "Metal", "Hide",
        "Arrowvine", "Axenut", "Corpsecap", "Flamefruit", "Rockroot", "Snowthistle",
    ];
}

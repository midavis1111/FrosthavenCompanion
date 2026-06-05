namespace FrosthavenCompanion.Domain;

/// <summary>
/// The derived state of a scenario for the current campaign progress. These are
/// computed from the scenario graph plus which scenarios have been completed —
/// they are never stored. <see cref="Hidden"/> scenarios are not shown to the
/// player at all, mirroring how the game reveals the flow chart as you go.
/// </summary>
public enum ScenarioStatus
{
    /// <summary>Not yet revealed — the player does not know it exists.</summary>
    Hidden,

    /// <summary>Revealed and playable now.</summary>
    Available,

    /// <summary>Revealed but gated by an unmet requirement.</summary>
    Locked,

    /// <summary>Permanently unavailable because a branching choice locked it out.</summary>
    LockedOut,

    /// <summary>Completed.</summary>
    Completed,
}

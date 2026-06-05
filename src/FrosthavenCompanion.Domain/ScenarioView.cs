namespace FrosthavenCompanion.Domain;

/// <summary>
/// A scenario definition paired with its derived <see cref="ScenarioStatus"/> for
/// the current progress. This is what the UI renders; it is recomputed on every
/// change rather than stored.
/// </summary>
public sealed record ScenarioView(ScenarioDefinition Definition, ScenarioStatus Status, DateOnly? CompletedOn)
{
    public string Index => Definition.Index;
    public string Name => Definition.Name;
}

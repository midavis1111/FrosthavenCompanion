namespace FrosthavenCompanion.Domain;

/// <summary>
/// Derives scenario visibility/availability from the scenario graph plus the
/// player's completed scenarios, and applies completion. This is where the
/// Frosthaven flow-chart rules live:
/// <list type="bullet">
///   <item>A scenario is revealed by being <c>initial</c> or by being in the
///   <c>unlocks</c>/<c>links</c> of a completed scenario.</item>
///   <item>Completing a scenario locks out everything in its <c>blocks</c> list
///   (the branch not taken).</item>
///   <item>A revealed scenario is playable once everything it <c>requires</c> is
///   completed; until then it shows as Locked.</item>
/// </list>
/// Status precedence: Completed &gt; LockedOut &gt; Hidden &gt; Locked/Available.
/// </summary>
public sealed class CampaignEngine(ScenarioCatalog catalog)
{
    public ScenarioCatalog Catalog => catalog;

    /// <summary>Computes the status of every scenario for the given progress.</summary>
    public IReadOnlyList<ScenarioView> BuildViews(CampaignProgress progress)
    {
        var completed = progress.Completed;

        // Scenarios revealed and scenarios locked out by what has been completed.
        var revealed = new HashSet<string>();
        var lockedOut = new HashSet<string>();
        foreach (var def in catalog.Scenarios)
        {
            if (def.Initial)
                revealed.Add(def.Index);

            if (!completed.ContainsKey(def.Index))
                continue;

            foreach (var u in def.Unlocks) revealed.Add(u);
            foreach (var l in def.Links) revealed.Add(l);
            foreach (var b in def.Blocks) lockedOut.Add(b);
        }

        return catalog.Scenarios
            .Select(def => new ScenarioView(
                def,
                Status(def, completed, revealed, lockedOut, progress.ManualUnlocks),
                completed.TryGetValue(def.Index, out var d) ? d : null))
            .ToList();
    }

    private static ScenarioStatus Status(
        ScenarioDefinition def,
        IReadOnlyDictionary<string, DateOnly> completed,
        IReadOnlySet<string> revealed,
        IReadOnlySet<string> lockedOut,
        IReadOnlyDictionary<string, string> manualUnlocks)
    {
        if (completed.ContainsKey(def.Index))
            return ScenarioStatus.Completed;
        // An explicit manual unlock (e.g. a personal quest) makes a scenario
        // playable regardless of the graph.
        if (manualUnlocks.ContainsKey(def.Index))
            return ScenarioStatus.Available;
        if (lockedOut.Contains(def.Index))
            return ScenarioStatus.LockedOut;
        if (!revealed.Contains(def.Index))
            return ScenarioStatus.Hidden;

        var requirementsMet = def.Requires.All(completed.ContainsKey);
        return requirementsMet ? ScenarioStatus.Available : ScenarioStatus.Locked;
    }

    /// <summary>True if the scenario can currently be completed (it is Available).</summary>
    public bool CanComplete(CampaignProgress progress, string index) =>
        BuildViews(progress).FirstOrDefault(v => v.Index == index)?.Status == ScenarioStatus.Available;

    /// <summary>
    /// Marks a scenario completed. Returns the scenarios newly revealed as a
    /// result (so the UI can announce them). Throws if the scenario is unknown.
    /// </summary>
    public IReadOnlyList<ScenarioDefinition> Complete(CampaignProgress progress, string index, DateOnly completedOn)
    {
        var def = catalog.Find(index)
            ?? throw new InvalidOperationException($"Scenario '{index}' is not in the catalog.");

        progress.Completed[index] = completedOn;

        // Of the scenarios this one points at, report those now visible (not blocked).
        var status = BuildViews(progress).ToDictionary(v => v.Index, v => v.Status);
        return def.Unlocks.Concat(def.Links).Distinct()
            .Select(catalog.Find)
            .OfType<ScenarioDefinition>()
            .Where(d => status.GetValueOrDefault(d.Index) is ScenarioStatus.Available or ScenarioStatus.Locked)
            .ToList();
    }

    /// <summary>Reverses a completion (undo).</summary>
    public void Uncomplete(CampaignProgress progress, string index) =>
        progress.Completed.Remove(index);

    /// <summary>
    /// Marks a scenario as unlocked by an external source (personal quest, event,
    /// item, ...). Returns the matching definition. Throws if the index is not a
    /// real Frosthaven scenario, so typos are caught.
    /// </summary>
    public ScenarioDefinition ManuallyUnlock(CampaignProgress progress, string index, string? source)
    {
        var def = catalog.Find(index)
            ?? throw new InvalidOperationException($"There is no scenario #{index}.");
        progress.ManualUnlocks[def.Index] = source?.Trim() ?? "";
        return def;
    }

    /// <summary>Removes a manual unlock (e.g. entered by mistake).</summary>
    public void RemoveManualUnlock(CampaignProgress progress, string index) =>
        progress.ManualUnlocks.Remove(index);

    /// <summary>The source note recorded for a manual unlock, if any.</summary>
    public static string? ManualUnlockSource(CampaignProgress progress, string index) =>
        progress.ManualUnlocks.TryGetValue(index, out var s) && !string.IsNullOrWhiteSpace(s) ? s : null;
}

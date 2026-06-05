using FrosthavenCompanion.Domain;
using Microsoft.JSInterop;

namespace FrosthavenCompanion.App.Services;

/// <summary>
/// Holds the player's <see cref="CampaignProgress"/> in memory, persists it to
/// the browser's localStorage, and exposes the derived scenario views via the
/// <see cref="CampaignEngine"/>. The UI only ever talks to this.
/// </summary>
public sealed class CampaignStore(CampaignEngine engine, IJSRuntime js)
{
    private const string StorageKey = "frosthaven.progress";

    public CampaignProgress Progress { get; private set; } = new();

    /// <summary>The full scenario graph, derived for the current progress.</summary>
    public IReadOnlyList<ScenarioView> Views => engine.BuildViews(Progress);

    public async Task LoadAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        Progress = string.IsNullOrWhiteSpace(json)
            ? new CampaignProgress()
            : CampaignSerializer.Deserialize(json);
    }

    public async Task SaveAsync()
    {
        var json = CampaignSerializer.Serialize(Progress);
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    /// <summary>Completes a scenario and returns the scenarios it newly revealed.</summary>
    public async Task<IReadOnlyList<ScenarioDefinition>> CompleteAsync(string index, DateOnly completedOn)
    {
        var revealed = engine.Complete(Progress, index, completedOn);
        await SaveAsync();
        return revealed;
    }

    public async Task UncompleteAsync(string index)
    {
        engine.Uncomplete(Progress, index);
        await SaveAsync();
    }

    public async Task ImportAsync(string json)
    {
        Progress = CampaignSerializer.Deserialize(json);
        await SaveAsync();
    }

    public string Export() => CampaignSerializer.Serialize(Progress);
}

using FrosthavenCompanion.Domain;
using Microsoft.JSInterop;

namespace FrosthavenCompanion.App.Services;

/// <summary>
/// Holds the player's <see cref="CampaignProgress"/> in memory, persists it to
/// the browser's localStorage, optionally mirrors it to a GitHub gist for
/// cross-device sync, and exposes the derived scenario views via the
/// <see cref="CampaignEngine"/>. The UI only ever talks to this.
/// </summary>
public sealed class CampaignStore(CampaignEngine engine, GistSyncService sync, IJSRuntime js)
{
    private const string StorageKey = "frosthaven.progress";
    private bool loaded;

    public CampaignProgress Progress { get; private set; } = new();

    public GistSyncService Sync => sync;

    /// <summary>Last sync outcome, shown in the UI. Null until a sync is attempted.</summary>
    public string? SyncStatus { get; private set; }

    /// <summary>The full scenario graph, derived for the current progress.</summary>
    public IReadOnlyList<ScenarioView> Views => engine.BuildViews(Progress);

    /// <summary>
    /// Loads progress from local storage and reconciles with the gist once per
    /// session. Safe to call from every page; only the first call does the work.
    /// </summary>
    public async Task LoadAsync()
    {
        if (loaded) return;
        Progress = await ReadLocalAsync() ?? new CampaignProgress();
        await sync.EnsureLoadedAsync();
        if (sync.Connected)
            await ReconcileWithRemoteAsync();
        if (EnsureCharacterIds())   // back-fill ids on saves written before they existed
            await SaveAsync();
        loaded = true;
    }

    private bool EnsureCharacterIds()
    {
        var changed = false;
        foreach (var c in Progress.Party.Concat(Progress.ArchivedCharacters).Where(c => string.IsNullOrEmpty(c.Id)))
        {
            c.Id = Guid.NewGuid().ToString("N");
            changed = true;
        }
        return changed;
    }

    /// <summary>Pulls the remote save and adopts whichever copy is newer.</summary>
    private async Task ReconcileWithRemoteAsync()
    {
        try
        {
            var remoteJson = await sync.PullAsync();
            if (remoteJson is not null)
            {
                var remote = CampaignSerializer.Deserialize(remoteJson);
                if (remote.UpdatedAt > Progress.UpdatedAt)
                {
                    Progress = remote;
                    await WriteLocalAsync();
                    SyncStatus = "Loaded the newer copy from the cloud.";
                    return;
                }
            }
            await sync.PushAsync(CampaignSerializer.Serialize(Progress));
            SyncStatus = "Synced.";
        }
        catch (Exception ex)
        {
            SyncStatus = $"Sync failed: {ex.Message}";
        }
    }

    private async Task<CampaignProgress?> ReadLocalAsync()
    {
        string? json;
        try
        {
            json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        }
        catch
        {
            // localStorage can be unavailable (e.g. private browsing); run in-memory.
            SyncStatus = "Local storage is unavailable — changes won't be saved on this device.";
            return null;
        }

        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return CampaignSerializer.Deserialize(json);
        }
        catch (FormatException)
        {
            // Don't brick the app on a corrupt save — keep a backup and start fresh.
            try { await js.InvokeVoidAsync("localStorage.setItem", $"{StorageKey}.corrupt", json); }
            catch { /* best effort */ }
            SyncStatus = "The saved campaign couldn't be read; it was backed up and a new one started.";
            return null;
        }
    }

    private async Task WriteLocalAsync() =>
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, CampaignSerializer.Serialize(Progress));

    /// <summary>Persists a change: stamps the time, writes locally, and pushes to the gist if connected.</summary>
    public async Task SaveAsync()
    {
        Progress.UpdatedAt = DateTimeOffset.UtcNow;
        await WriteLocalAsync();
        if (sync.Connected)
        {
            try
            {
                await sync.PushAsync(CampaignSerializer.Serialize(Progress));
                SyncStatus = "Synced.";
            }
            catch (Exception ex)
            {
                SyncStatus = $"Sync failed: {ex.Message}";
            }
        }
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

    /// <summary>Manually unlocks a scenario from an external source. Returns its definition.</summary>
    public async Task<ScenarioDefinition> ManuallyUnlockAsync(string index, string? source)
    {
        var def = engine.ManuallyUnlock(Progress, index, source);
        await SaveAsync();
        return def;
    }

    public async Task RemoveManualUnlockAsync(string index)
    {
        engine.RemoveManualUnlock(Progress, index);
        await SaveAsync();
    }

    /// <summary>The remembered play level for a scenario, or the campaign default.</summary>
    public int ScenarioLevelFor(string index) =>
        Progress.ScenarioLevels.GetValueOrDefault(index, Progress.ScenarioLevel);

    /// <summary>Stores a scenario's chosen level and makes it the new campaign default.</summary>
    public async Task SetScenarioLevelAsync(string index, int level)
    {
        Progress.ScenarioLevels[index] = level;
        Progress.ScenarioLevel = level;
        await SaveAsync();
    }

    /// <summary>Adds a blank character to the party.</summary>
    public async Task AddCharacterAsync()
    {
        Progress.Party.Add(new Character { Id = Guid.NewGuid().ToString("N") });
        await SaveAsync();
    }

    /// <summary>The player's own character (their selection, or the first party member).</summary>
    public Character? MyCharacter =>
        Progress.Party.FirstOrDefault(c => c.Id == Progress.MyCharacterId)
        ?? Progress.Party.FirstOrDefault();

    /// <summary>Marks which party member is the player's own character.</summary>
    public async Task SetMyCharacterAsync(string id)
    {
        Progress.MyCharacterId = id;
        await SaveAsync();
    }

    /// <summary>Class names the player has a saved sheet for (active + archived).</summary>
    public IEnumerable<string> MyClasses =>
        Progress.ArchivedCharacters
            .Select(c => c.ClassName)
            .Append(MyCharacter?.ClassName ?? "")
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Switches the player's own character to the given class without losing data:
    /// the current sheet is archived and the target class's saved sheet is restored
    /// (or a fresh one created). Each class keeps its own perks, cards, level, etc.
    /// </summary>
    public async Task SwitchMyClassAsync(string className)
    {
        className = className.Trim();
        if (string.IsNullOrEmpty(className)) return;

        var cur = MyCharacter;
        if (cur is not null && string.Equals(cur.ClassName, className, StringComparison.OrdinalIgnoreCase))
            return;

        // Stash the current sheet (if it's a real, classed character).
        if (cur is not null && !string.IsNullOrWhiteSpace(cur.ClassName))
        {
            Progress.Party.Remove(cur);
            Progress.ArchivedCharacters.Add(cur);
        }

        // Restore the target class from the archive, or reuse an empty current, or make a new one.
        var target = Progress.ArchivedCharacters
            .FirstOrDefault(c => string.Equals(c.ClassName, className, StringComparison.OrdinalIgnoreCase));
        if (target is not null)
            Progress.ArchivedCharacters.Remove(target);
        else if (cur is not null && string.IsNullOrWhiteSpace(cur.ClassName))
            target = cur;   // an unclassed starter row — just name its class
        else
            target = new Character { Id = Guid.NewGuid().ToString("N") };

        target.ClassName = className;
        if (!Progress.Party.Contains(target)) Progress.Party.Add(target);
        Progress.MyCharacterId = target.Id;
        await SaveAsync();
    }

    /// <summary>Sets the player's Blinkblade time-token (hourglass) count (floored at 0).</summary>
    public async Task SetTimeTokensAsync(Character c, int count)
    {
        c.TimeTokens = Math.Max(0, count);
        await SaveAsync();
    }

    public int ResourceOf(Character c, string slug) => c.Resources.GetValueOrDefault(slug);

    /// <summary>Sets a personal resource count (clamped at 0; 0 forgets it).</summary>
    public async Task SetResourceAsync(Character c, string slug, int count)
    {
        count = Math.Max(0, count);
        if (count == 0) c.Resources.Remove(slug);
        else c.Resources[slug] = count;
        await SaveAsync();
    }

    public async Task RemoveCharacterAsync(Character character)
    {
        Progress.Party.Remove(character);
        await SaveAsync();
    }

    /// <summary>Retirements, newest first.</summary>
    public IReadOnlyList<Retirement> Retirements =>
        Progress.Retirements.OrderByDescending(r => r.Date).ToList();

    public async Task AddRetirementAsync(Retirement retirement)
    {
        Progress.Retirements.Add(retirement);
        await SaveAsync();
    }

    public async Task RemoveRetirementAsync(Retirement retirement)
    {
        Progress.Retirements.Remove(retirement);
        await SaveAsync();
    }

    /// <summary>The mark on a perk checkbox, or null if it's unchecked.</summary>
    public static PerkMark? PerkState(Character c, int perkIndex, int boxIndex) =>
        c.Perks.TryGetValue($"{perkIndex}:{boxIndex}", out var m) ? m : null;

    /// <summary>Cycles a perk checkbox: unchecked → taken → bonus (from card) → unchecked.</summary>
    public async Task CyclePerkAsync(Character c, int perkIndex, int boxIndex)
    {
        var key = $"{perkIndex}:{boxIndex}";
        switch (PerkState(c, perkIndex, boxIndex))
        {
            case null: c.Perks[key] = PerkMark.Taken; break;
            case PerkMark.Taken: c.Perks[key] = PerkMark.Bonus; break;
            default: c.Perks.Remove(key); break;   // Bonus → unchecked
        }
        await SaveAsync();
    }

    /// <summary>An owned card's slot (Deck/Bench), or null if not owned/unset.</summary>
    public static CardSlot? CardSlotOf(Character c, int cardId) =>
        c.Cards.TryGetValue(cardId, out var s) ? s : null;

    /// <summary>Sets a card's slot directly; null removes it (unowned).</summary>
    public async Task SetCardSlotAsync(Character c, int cardId, CardSlot? slot)
    {
        if (slot is null) c.Cards.Remove(cardId);
        else c.Cards[cardId] = slot.Value;
        await SaveAsync();
    }

    /// <summary>Saves a named deck set (loadout) from the given card ids.</summary>
    public async Task AddCardSetAsync(Character c, string name, IEnumerable<int> cardIds)
    {
        c.CardSets.Add(new CardSet { Name = name.Trim(), CardIds = cardIds.Distinct().ToList() });
        await SaveAsync();
    }

    public async Task DeleteCardSetAsync(Character c, CardSet set)
    {
        c.CardSets.Remove(set);
        await SaveAsync();
    }

    /// <summary>Replaces the active deck with a set: its cards → Deck, any other deck cards → Bench.</summary>
    public async Task ApplyCardSetAsync(Character c, IEnumerable<int> cardIds)
    {
        var target = cardIds.ToHashSet();
        foreach (var id in c.Cards.Where(kv => kv.Value == CardSlot.Deck && !target.Contains(kv.Key))
                     .Select(kv => kv.Key).ToList())
            c.Cards[id] = CardSlot.Bench;
        foreach (var id in target)
            c.Cards[id] = CardSlot.Deck;
        await SaveAsync();
    }

    /// <summary>Cycles an ability card: unowned → Bench → Deck → unowned.</summary>
    public async Task CycleCardAsync(Character c, int cardId)
    {
        switch (CardSlotOf(c, cardId))
        {
            case null: c.Cards[cardId] = CardSlot.Bench; break;
            case CardSlot.Bench: c.Cards[cardId] = CardSlot.Deck; break;
            default: c.Cards.Remove(cardId); break;   // Deck → unowned
        }
        await SaveAsync();
    }

    /// <summary>A deck card's play pile, or null if it's in hand.</summary>
    public static PlayPile? PlayPileOf(Character c, int cardId) =>
        c.Play.TryGetValue(cardId, out var p) ? p : null;

    /// <summary>Moves a card to a play pile; null returns it to hand.</summary>
    public async Task SetPlayPileAsync(Character c, int cardId, PlayPile? pile)
    {
        if (pile is null) c.Play.Remove(cardId);
        else c.Play[cardId] = pile.Value;
        await SaveAsync();
    }

    /// <summary>Plays a round: sends the chosen cards to one pile (discard by default) in a single save.</summary>
    public async Task PlayRoundAsync(Character c, IEnumerable<int> cardIds, PlayPile pile = PlayPile.Discard)
    {
        foreach (var id in cardIds) c.Play[id] = pile;
        await SaveAsync();
    }

    /// <summary>Returns all discarded cards to hand.</summary>
    public async Task RecoverDiscardsAsync(Character c)
    {
        foreach (var id in c.Play.Where(kv => kv.Value == PlayPile.Discard).Select(kv => kv.Key).ToList())
            c.Play.Remove(id);
        await SaveAsync();
    }

    /// <summary>Short rest: recover discards to hand and lose one at random. Returns the lost card id, or null.</summary>
    public async Task<int?> ShortRestAsync(Character c)
    {
        var discarded = c.Play.Where(kv => kv.Value == PlayPile.Discard).Select(kv => kv.Key).ToList();
        foreach (var id in discarded) c.Play.Remove(id);

        int? lost = null;
        if (discarded.Count > 0)
        {
            lost = discarded[Random.Shared.Next(discarded.Count)];
            c.Play[lost.Value] = PlayPile.Lost;
        }
        await SaveAsync();
        return lost;
    }

    /// <summary>Returns every deck card to hand (start of a new scenario).</summary>
    public async Task ResetPlayAsync(Character c)
    {
        c.Play.Clear();
        await SaveAsync();
    }

    public static bool IsMastery(Character c, int index) => c.Masteries.Contains(index);

    /// <summary>Checks or unchecks a mastery.</summary>
    public async Task ToggleMasteryAsync(Character c, int index)
    {
        if (!c.Masteries.Add(index))
            c.Masteries.Remove(index);
        await SaveAsync();
    }

    /// <summary>Sets the difficulty offset from the recommended scenario level (−2…+3).</summary>
    public async Task SetDifficultyAsync(int modifier)
    {
        Progress.DifficultyModifier = Math.Clamp(modifier, -2, 3);
        await SaveAsync();
    }

    /// <summary>The stored state for a building (a fresh not-built default if untouched).</summary>
    public BuildingProgress BuildingOf(string slug) =>
        Progress.Buildings.GetValueOrDefault(slug) ?? new BuildingProgress();

    /// <summary>Records a building's level and condition. Level ≤ 0 forgets it (not built).</summary>
    public async Task SetBuildingAsync(string slug, int level, BuildingCondition condition)
    {
        if (level <= 0)
        {
            Progress.Buildings.Remove(slug);
        }
        else
        {
            var b = Progress.Buildings.TryGetValue(slug, out var existing) ? existing : new BuildingProgress();
            b.Level = level;
            b.Condition = condition;
            Progress.Buildings[slug] = b;
        }
        await SaveAsync();
    }

    public bool IsOutpostStepDone(string key) => Progress.OutpostChecklist.Contains(key);

    /// <summary>Toggles an outpost-phase checklist step.</summary>
    public async Task ToggleOutpostStepAsync(string key)
    {
        if (!Progress.OutpostChecklist.Add(key))
            Progress.OutpostChecklist.Remove(key);
        await SaveAsync();
    }

    /// <summary>Clears every checklist tick to start a new outpost phase.</summary>
    public async Task ResetOutpostChecklistAsync()
    {
        Progress.OutpostChecklist.Clear();
        await SaveAsync();
    }

    /// <summary>How many times a scenario has been lost/retried.</summary>
    public int ScenarioLossesOf(string index) => Progress.ScenarioLosses.GetValueOrDefault(index);

    /// <summary>Sets a scenario's loss count (clamped at 0; 0 forgets it).</summary>
    public async Task SetScenarioLossesAsync(string index, int losses)
    {
        losses = Math.Max(0, losses);
        if (losses == 0)
            Progress.ScenarioLosses.Remove(index);
        else
            Progress.ScenarioLosses[index] = losses;
        await SaveAsync();
    }

    public bool IsManualUnlock(string index) => Progress.ManualUnlocks.ContainsKey(index);

    public string? ManualUnlockSource(string index) => CampaignEngine.ManualUnlockSource(Progress, index);

    /// <summary>Explains how a scenario became known (start / manual / completed-by).</summary>
    public string? UnlockExplanation(string index) => engine.UnlockExplanation(Progress, index);

    public async Task ImportAsync(string json)
    {
        Progress = CampaignSerializer.Deserialize(json);
        await SaveAsync();
    }

    public string Export() => CampaignSerializer.Serialize(Progress);

    /// <summary>Connects gist sync with the given token and reconciles immediately.</summary>
    public async Task ConnectSyncAsync(string token)
    {
        var found = await sync.ConnectAsync(token);
        if (found)
            await ReconcileWithRemoteAsync();
        else
            await sync.CreateAsync(Export()); // first device — seed the gist with local data
        SyncStatus = "Connected and synced.";
    }

    public async Task DisconnectSyncAsync()
    {
        await sync.DisconnectAsync();
        SyncStatus = null;
    }

    /// <summary>Manual pull/push, reconciling to the newer copy.</summary>
    public async Task SyncNowAsync() => await ReconcileWithRemoteAsync();
}

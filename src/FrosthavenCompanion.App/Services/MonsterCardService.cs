using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace FrosthavenCompanion.App.Services;

/// <summary>
/// Resolves the official Frosthaven monster stat-card image for a monster at a
/// given scenario level. Each monster's cards are split into level bands (normal
/// monsters: 0 and 4; bosses: 0/2/4/6), described by the embedded
/// <c>icons/monster-cards/manifest.json</c> (slug → available bands). Monsters
/// with no card (most bosses / named one-offs) resolve to null, and the UI falls
/// back to the data table.
/// </summary>
public sealed partial class MonsterCardService(HttpClient http)
{
    private Dictionary<string, int[]>? _bands;

    /// <summary>Loads the band manifest once. Safe to call from every page.</summary>
    public async Task EnsureLoadedAsync()
    {
        if (_bands is not null) return;
        try
        {
            _bands = await http.GetFromJsonAsync<Dictionary<string, int[]>>("icons/monster-cards/manifest.json")
                     ?? [];
        }
        catch
        {
            _bands = []; // No manifest → everything falls back to the table.
        }
    }

    /// <summary>
    /// The stat-card image URL for <paramref name="monsterSlug"/> at
    /// <paramref name="level"/> (the highest band ≤ level), or null if the monster
    /// has no card art. Call <see cref="EnsureLoadedAsync"/> first.
    /// </summary>
    public string? CardImage(string monsterSlug, int level)
    {
        if (_bands is null) return null;
        var slug = BaseSlug(monsterSlug);
        if (!_bands.TryGetValue(slug, out var bands) || bands.Length == 0) return null;
        var band = bands.Where(b => b <= level).DefaultIfEmpty(bands[0]).Max();
        return $"icons/monster-cards/{slug}-{band}.png";
    }

    // Scenario/section variants ("frozen-corpse-scenario-118") reuse the base card.
    private static string BaseSlug(string name) => VariantSuffix().Replace(name, "");

    [GeneratedRegex("-(scenario|section)-.*$")]
    private static partial Regex VariantSuffix();
}

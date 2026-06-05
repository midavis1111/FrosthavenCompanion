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

    /// <summary>
    /// Degrees to rotate the stat card so <paramref name="level"/> reads upright
    /// (the card prints its band's four levels one per edge). Returns 0 when the
    /// monster has no card, or for bosses whose cards aren't the standard
    /// four-levels-per-card layout (left upright).
    /// </summary>
    public int CardRotation(string monsterSlug, int level)
    {
        if (_bands is null) return 0;
        var slug = BaseSlug(monsterSlug);
        if (!_bands.TryGetValue(slug, out var bands) || bands.Length == 0) return 0;

        var band = bands.Where(b => b <= level).DefaultIfEmpty(bands[0]).Max();
        var idx = Array.IndexOf(bands, band);
        var next = idx + 1 < bands.Length ? bands[idx + 1] : band + 4;
        if (next - band < 4) return 0; // boss cards span fewer levels — don't rotate.

        // Level b is upright at top; each step clockwise round the card is −90°.
        return (-90 * (level - band) + 360) % 360;
    }

    // Scenario/section variants ("frozen-corpse-scenario-118") reuse the base card.
    private static string BaseSlug(string name) => VariantSuffix().Replace(name, "");

    [GeneratedRegex("-(scenario|section)-.*$")]
    private static partial Regex VariantSuffix();
}

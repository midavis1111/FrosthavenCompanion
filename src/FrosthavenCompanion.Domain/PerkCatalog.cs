using System.Text.Json;

namespace FrosthavenCompanion.Domain;

/// <summary>One perk row on a class's perk sheet: its text and how many checkboxes it has.</summary>
public sealed record PerkDefinition
{
    public required string Text { get; init; }
    public int Boxes { get; init; } = 1;
}

/// <summary>
/// Per-class perk lists, loaded from the embedded perks.json. Keyed by class slug
/// (e.g. "blinkblade"); <see cref="For"/> accepts a display name like "Blinkblade"
/// and slugifies it. Only classes that have been authored are present.
/// </summary>
public sealed class PerkCatalog
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<PerkDefinition>> _byClass;

    public PerkCatalog(IReadOnlyDictionary<string, IReadOnlyList<PerkDefinition>> byClass) => _byClass = byClass;

    /// <summary>The perk list for a class display name or slug, or empty if not authored.</summary>
    public IReadOnlyList<PerkDefinition> For(string classNameOrSlug) =>
        _byClass.GetValueOrDefault(Slugify(classNameOrSlug)) ?? [];

    public bool Has(string classNameOrSlug) => For(classNameOrSlug).Count > 0;

    /// <summary>Turns "Banner Spear" into "banner-spear" to match the data keys.</summary>
    public static string Slugify(string name) =>
        string.Join('-', name.Trim().ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static PerkCatalog LoadEmbedded()
    {
        var assembly = typeof(PerkCatalog).Assembly;
        const string resourceName = "FrosthavenCompanion.Domain.Data.perks.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");

        var data = JsonSerializer.Deserialize<Dictionary<string, List<PerkDefinition>>>(stream, Options)
            ?? throw new InvalidOperationException("The embedded perk catalog could not be read.");

        var byClass = data.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<PerkDefinition>)kv.Value,
            StringComparer.OrdinalIgnoreCase);
        return new PerkCatalog(byClass);
    }
}

using System.Text.Json;

namespace FrosthavenCompanion.Domain;

/// <summary>One perk row on a class's perk sheet: its text and how many checkboxes it has.</summary>
public sealed record PerkDefinition
{
    public required string Text { get; init; }
    public int Boxes { get; init; } = 1;
}

/// <summary>
/// Per-class perks and masteries, loaded from the embedded perks.json. Keyed by
/// class slug (e.g. "blinkblade"); the lookups accept a display name like
/// "Blinkblade" and slugify it. Only classes that have been authored are present.
/// Perk/mastery text may contain {slug} icon tokens (see the IconText component).
/// </summary>
public sealed class PerkCatalog
{
    private sealed class ClassEntry
    {
        public List<PerkDefinition> Perks { get; init; } = [];
        public List<string> Masteries { get; init; } = [];
    }

    private readonly IReadOnlyDictionary<string, ClassEntry> _byClass;

    private PerkCatalog(IReadOnlyDictionary<string, ClassEntry> byClass) => _byClass = byClass;

    /// <summary>The perk rows for a class display name or slug, or empty if not authored.</summary>
    public IReadOnlyList<PerkDefinition> For(string classNameOrSlug) =>
        _byClass.TryGetValue(Slugify(classNameOrSlug), out var e) ? e.Perks : [];

    /// <summary>The mastery lines for a class display name or slug, or empty if not authored.</summary>
    public IReadOnlyList<string> Masteries(string classNameOrSlug) =>
        _byClass.TryGetValue(Slugify(classNameOrSlug), out var e) ? e.Masteries : [];

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

        var data = JsonSerializer.Deserialize<Dictionary<string, ClassEntry>>(stream, Options)
            ?? throw new InvalidOperationException("The embedded perk catalog could not be read.");

        var byClass = new Dictionary<string, ClassEntry>(data, StringComparer.OrdinalIgnoreCase);
        return new PerkCatalog(byClass);
    }
}

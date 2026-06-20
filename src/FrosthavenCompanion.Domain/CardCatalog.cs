using System.Text.Json;

namespace FrosthavenCompanion.Domain;

/// <summary>One ability card: its stable id, name, level ("1", "X", "2"–"9"), and initiative.</summary>
public sealed record AbilityCard
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string Level { get; init; } = "1";

    /// <summary>Raw initiative. Blinkblade cards encode fast/slow as one 4-digit number (e.g. 2050 = 20/50).</summary>
    public int Initiative { get; init; }
}

/// <summary>
/// Per-class ability-card lists (+ hand size), loaded from the embedded cards.json.
/// Keyed by class slug; <see cref="For"/> accepts a display name and slugifies it.
/// Only authored classes are present.
/// </summary>
public sealed class CardCatalog
{
    private sealed class ClassDeck
    {
        public int HandSize { get; init; }
        public List<AbilityCard> Cards { get; init; } = [];
    }

    private readonly IReadOnlyDictionary<string, ClassDeck> _byClass;

    private CardCatalog(IReadOnlyDictionary<string, ClassDeck> byClass) => _byClass = byClass;

    /// <summary>The ability cards for a class display name or slug, or empty if not authored.</summary>
    public IReadOnlyList<AbilityCard> For(string classNameOrSlug) =>
        _byClass.TryGetValue(PerkCatalog.Slugify(classNameOrSlug), out var d) ? d.Cards : [];

    /// <summary>The class's hand size (number of cards in a scenario deck), or 0 if unknown.</summary>
    public int HandSize(string classNameOrSlug) =>
        _byClass.TryGetValue(PerkCatalog.Slugify(classNameOrSlug), out var d) ? d.HandSize : 0;

    public bool Has(string classNameOrSlug) => For(classNameOrSlug).Count > 0;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static CardCatalog LoadEmbedded()
    {
        var assembly = typeof(CardCatalog).Assembly;
        const string resourceName = "FrosthavenCompanion.Domain.Data.cards.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");

        var data = JsonSerializer.Deserialize<Dictionary<string, ClassDeck>>(stream, Options)
            ?? throw new InvalidOperationException("The embedded card catalog could not be read.");

        return new CardCatalog(new Dictionary<string, ClassDeck>(data, StringComparer.OrdinalIgnoreCase));
    }
}

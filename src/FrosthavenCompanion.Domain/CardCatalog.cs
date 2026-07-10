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

    /// <summary>Card-art file slug under wwwroot/icons/cards/{classSlug}/, or null if no image.</summary>
    public string? Image { get; init; }

    /// <summary>Authored effect tags (conditions, elements, keywords) this card provides. Merged from card-tags.json.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Initiative for display. Blinkblade cards encode a fast/slow pair as one number
    /// (slow in the last two digits), e.g. 2050 → "20/50" and 232 → "2/32". Normal
    /// initiatives (1–99) show as-is.
    /// </summary>
    public string InitiativeDisplay => Initiative >= 100 ? $"{Initiative / 100}/{Initiative % 100:00}" : Initiative.ToString();
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

    /// <summary>All class slugs that have an authored deck (e.g. "blinkblade").</summary>
    public IReadOnlyCollection<string> Classes => _byClass.Keys.OrderBy(k => k).ToList();

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

        MergeTags(assembly, data);

        return new CardCatalog(new Dictionary<string, ClassDeck>(data, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Applies authored per-card effect tags (card-tags.json, class → card name → tags).</summary>
    private static void MergeTags(System.Reflection.Assembly assembly, Dictionary<string, ClassDeck> data)
    {
        using var stream = assembly.GetManifestResourceStream("FrosthavenCompanion.Domain.Data.card-tags.json");
        if (stream is null) return;

        var tags = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(stream, Options);
        if (tags is null) return;

        foreach (var (classSlug, byName) in tags)
        {
            if (!data.TryGetValue(classSlug, out var deck)) continue;
            for (var i = 0; i < deck.Cards.Count; i++)
            {
                if (byName.TryGetValue(deck.Cards[i].Name, out var cardTags))
                    deck.Cards[i] = deck.Cards[i] with { Tags = cardTags };
            }
        }
    }
}

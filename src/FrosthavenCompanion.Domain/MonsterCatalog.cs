using System.Text.Json;

namespace FrosthavenCompanion.Domain;

/// <summary>
/// All monster stat blocks, loaded from the embedded monsters.json. Looked up by
/// slug (e.g. "algox-archer") or display name (e.g. "Algox Archer") so scenario
/// enemy lists can link straight to a monster.
/// </summary>
public sealed class MonsterCatalog
{
    private readonly Dictionary<string, MonsterStats> _byName;
    private readonly Dictionary<string, MonsterStats> _byDisplay;

    public MonsterCatalog(IEnumerable<MonsterStats> monsters)
    {
        Monsters = monsters.OrderBy(m => m.DisplayName).ToList();
        _byName = Monsters.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        _byDisplay = Monsters
            .GroupBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<MonsterStats> Monsters { get; }

    /// <summary>Finds a monster by slug or display name (case-insensitive).</summary>
    public MonsterStats? Find(string nameOrDisplay) =>
        _byName.GetValueOrDefault(nameOrDisplay) ?? _byDisplay.GetValueOrDefault(nameOrDisplay);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static MonsterCatalog LoadEmbedded()
    {
        var assembly = typeof(MonsterCatalog).Assembly;
        const string resourceName = "FrosthavenCompanion.Domain.Data.monsters.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");

        var monsters = JsonSerializer.Deserialize<List<MonsterStats>>(stream, Options)
            ?? throw new InvalidOperationException("The embedded monster catalog could not be read.");

        return new MonsterCatalog(monsters);
    }
}

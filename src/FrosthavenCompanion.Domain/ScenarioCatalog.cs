using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrosthavenCompanion.Domain;

/// <summary>
/// The full set of Frosthaven scenario definitions (the unlock graph). Loaded
/// once from the embedded catalog and treated as read-only reference data.
/// </summary>
public sealed class ScenarioCatalog
{
    private readonly Dictionary<string, ScenarioDefinition> _byIndex;

    public ScenarioCatalog(IEnumerable<ScenarioDefinition> scenarios)
    {
        Scenarios = scenarios.ToList();
        _byIndex = Scenarios.ToDictionary(s => s.Index);
    }

    public IReadOnlyList<ScenarioDefinition> Scenarios { get; }

    public ScenarioDefinition? Find(string index) =>
        _byIndex.GetValueOrDefault(index);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Loads the catalog from the JSON embedded in this assembly.</summary>
    public static ScenarioCatalog LoadEmbedded()
    {
        var assembly = typeof(ScenarioCatalog).Assembly;
        const string resourceName = "FrosthavenCompanion.Domain.Data.scenarios.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded catalog '{resourceName}' was not found.");

        var scenarios = JsonSerializer.Deserialize<List<ScenarioDefinition>>(stream, Options)
            ?? throw new InvalidOperationException("The embedded scenario catalog could not be read.");

        return new ScenarioCatalog(scenarios);
    }
}

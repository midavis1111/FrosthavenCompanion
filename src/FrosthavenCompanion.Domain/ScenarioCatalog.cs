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

    /// <summary>Loads the catalog from the JSON embedded in this assembly,
    /// merging in authored scenario descriptions from the separate file.</summary>
    public static ScenarioCatalog LoadEmbedded()
    {
        var assembly = typeof(ScenarioCatalog).Assembly;

        var scenarios = ReadEmbedded<List<ScenarioDefinition>>(assembly, "FrosthavenCompanion.Domain.Data.scenarios.json")
            ?? throw new InvalidOperationException("The embedded scenario catalog could not be read.");

        // Descriptions live in a separate file so regenerating the catalog never loses them.
        var descriptions = ReadEmbedded<Dictionary<string, string>>(assembly, "FrosthavenCompanion.Domain.Data.descriptions.json")
            ?? [];

        var merged = scenarios
            .Select(s => descriptions.TryGetValue(s.Index, out var d) ? s with { Description = d } : s)
            .ToList();

        return new ScenarioCatalog(merged);
    }

    private static T? ReadEmbedded<T>(System.Reflection.Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        return JsonSerializer.Deserialize<T>(stream, Options);
    }
}

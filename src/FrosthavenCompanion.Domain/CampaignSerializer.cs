using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrosthavenCompanion.Domain;

/// <summary>
/// Serializes <see cref="CampaignProgress"/> to and from JSON. This backs both
/// local browser storage and the export/import (backup) feature.
/// </summary>
public static class CampaignSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(CampaignProgress progress) =>
        JsonSerializer.Serialize(progress, Options);

    public static CampaignProgress Deserialize(string json) =>
        JsonSerializer.Deserialize<CampaignProgress>(json, Options)
        ?? throw new FormatException("The save data could not be read as campaign progress.");
}

using FrosthavenCompanion.Domain;

namespace FrosthavenCompanion.Domain.Tests;

public class OutpostTests
{
    [Fact]
    public void Building_catalog_lists_the_buildable_buildings_and_finds_by_slug()
    {
        Assert.Equal(22, BuildingCatalog.All.Count);
        var townHall = BuildingCatalog.Find("town-hall");
        Assert.NotNull(townHall);
        Assert.Equal("90", townHall.Number);
        Assert.Equal(3, townHall.MaxLevel);
        Assert.Null(BuildingCatalog.Find("nonexistent"));
    }

    [Fact]
    public void Outpost_checklist_has_ordered_stages()
    {
        Assert.NotEmpty(OutpostPhase.Steps);
        Assert.Equal("Passage of Time", OutpostPhase.Groups[0]);
        Assert.Contains("Downtime", OutpostPhase.Groups);
        // Step keys are unique (used as save keys).
        Assert.Equal(OutpostPhase.Steps.Count, OutpostPhase.Steps.Select(s => s.Key).Distinct().Count());
    }

    [Fact]
    public void Buildings_and_checklist_round_trip_through_serialization()
    {
        var progress = new CampaignProgress();
        progress.Buildings["mining-camp"] = new BuildingProgress { Level = 2, Condition = BuildingCondition.Damaged };
        progress.OutpostChecklist.Add("oe-draw");
        progress.OutpostChecklist.Add("bo-build");

        var restored = CampaignSerializer.Deserialize(CampaignSerializer.Serialize(progress));

        var mine = restored.Buildings["mining-camp"];
        Assert.Equal(2, mine.Level);
        Assert.Equal(BuildingCondition.Damaged, mine.Condition);
        Assert.Contains("oe-draw", restored.OutpostChecklist);
        Assert.Equal(2, restored.OutpostChecklist.Count);
    }

    [Fact]
    public void Saves_written_before_the_outpost_features_load_empty()
    {
        const string legacy = """{"partyName":"Old Party","completed":{}}""";

        var progress = CampaignSerializer.Deserialize(legacy);

        Assert.Empty(progress.Buildings);
        Assert.Empty(progress.OutpostChecklist);
    }
}

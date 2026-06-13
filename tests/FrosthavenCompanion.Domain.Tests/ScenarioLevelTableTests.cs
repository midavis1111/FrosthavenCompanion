using FrosthavenCompanion.Domain;

namespace FrosthavenCompanion.Domain.Tests;

public class ScenarioLevelTableTests
{
    [Fact]
    public void Empty_party_defaults_to_level_one()
    {
        Assert.Equal(1, ScenarioLevelTable.Recommended([]));
    }

    [Theory]
    [InlineData(new[] { 2, 2, 2, 2 }, 1)] // avg 2 / 2 = 1
    [InlineData(new[] { 3 }, 2)]          // avg 3 / 2 = 1.5 -> ceil 2
    [InlineData(new[] { 1, 2 }, 1)]       // avg 1.5 / 2 = 0.75 -> ceil 1
    [InlineData(new[] { 9, 9 }, 5)]       // avg 9 / 2 = 4.5 -> ceil 5
    [InlineData(new[] { 4, 5 }, 3)]       // avg 4.5 / 2 = 2.25 -> ceil 3
    public void Recommended_is_average_level_over_two_rounded_up(int[] levels, int expected)
    {
        Assert.Equal(expected, ScenarioLevelTable.Recommended(levels));
    }

    [Theory]
    [InlineData(new[] { 4, 4 }, -2, 0)] // recommended 2, -2 -> 0
    [InlineData(new[] { 4, 4 }, 3, 5)]  // recommended 2, +3 -> 5
    [InlineData(new[] { 9, 9 }, 3, 7)]  // recommended 5, +3 -> 8 clamped to 7
    public void Adjusted_applies_the_modifier_and_clamps(int[] levels, int modifier, int expected)
    {
        Assert.Equal(expected, ScenarioLevelTable.Adjusted(levels, modifier));
    }

    [Fact]
    public void Chart_matches_the_rulebook_at_the_extremes()
    {
        var lvl0 = ScenarioLevelTable.Info(0);
        Assert.Equal((2, 2, 1, 4), (lvl0.GoldConversion, lvl0.TrapDamage, lvl0.HazardousTerrain, lvl0.BonusExperience));

        var lvl7 = ScenarioLevelTable.Info(7);
        Assert.Equal((6, 9, 4, 18), (lvl7.GoldConversion, lvl7.TrapDamage, lvl7.HazardousTerrain, lvl7.BonusExperience));

        // Out-of-range levels clamp rather than throw.
        Assert.Equal(7, ScenarioLevelTable.Info(99).Level);
        Assert.Equal(0, ScenarioLevelTable.Info(-3).Level);
    }

    [Fact]
    public void Party_round_trips_through_serialization()
    {
        var progress = new CampaignProgress();
        progress.Party.Add(new Character { Name = "Mara", ClassName = "Drifter", Level = 4 });
        progress.DifficultyModifier = 1;

        var restored = CampaignSerializer.Deserialize(CampaignSerializer.Serialize(progress));

        Assert.Single(restored.Party);
        Assert.Equal("Drifter", restored.Party[0].ClassName);
        Assert.Equal(4, restored.Party[0].Level);
        Assert.Equal(1, restored.DifficultyModifier);
    }
}

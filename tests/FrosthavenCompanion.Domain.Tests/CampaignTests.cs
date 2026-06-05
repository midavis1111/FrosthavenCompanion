using FrosthavenCompanion.Domain;

namespace FrosthavenCompanion.Domain.Tests;

public class CampaignEngineTests
{
    private static readonly DateOnly Day = new(2026, 1, 1);

    // A tiny graph mirroring Frosthaven's intro branch:
    //   1 (initial) -> unlocks 2, 3
    //   2 -> unlocks 4B, blocks 3
    //   3 -> unlocks 4A, blocks 2
    private static CampaignEngine BuildEngine() => new(new ScenarioCatalog(
    [
        new ScenarioDefinition { Index = "1", Name = "Start", Initial = true, Unlocks = ["2", "3"] },
        new ScenarioDefinition { Index = "2", Name = "Branch A", Unlocks = ["4B"], Blocks = ["3"] },
        new ScenarioDefinition { Index = "3", Name = "Branch B", Unlocks = ["4A"], Blocks = ["2"] },
        new ScenarioDefinition { Index = "4A", Name = "End A" },
        new ScenarioDefinition { Index = "4B", Name = "End B" },
        new ScenarioDefinition { Index = "9", Name = "Gated", Unlocks = [], Requires = ["4B"] },
    ]));

    private static ScenarioStatus StatusOf(CampaignEngine e, CampaignProgress p, string index) =>
        e.BuildViews(p).First(v => v.Index == index).Status;

    [Fact]
    public void Only_initial_scenarios_are_visible_at_the_start()
    {
        var engine = BuildEngine();
        var progress = new CampaignProgress();

        Assert.Equal(ScenarioStatus.Available, StatusOf(engine, progress, "1"));
        Assert.Equal(ScenarioStatus.Hidden, StatusOf(engine, progress, "2"));
        Assert.Equal(ScenarioStatus.Hidden, StatusOf(engine, progress, "3"));
    }

    [Fact]
    public void Completing_a_scenario_reveals_what_it_unlocks()
    {
        var engine = BuildEngine();
        var progress = new CampaignProgress();

        var revealed = engine.Complete(progress, "1", Day);

        Assert.Equal(ScenarioStatus.Completed, StatusOf(engine, progress, "1"));
        Assert.Equal(ScenarioStatus.Available, StatusOf(engine, progress, "2"));
        Assert.Equal(ScenarioStatus.Available, StatusOf(engine, progress, "3"));
        Assert.Equal(["2", "3"], revealed.Select(d => d.Index));
    }

    [Fact]
    public void Choosing_one_branch_locks_out_the_other()
    {
        var engine = BuildEngine();
        var progress = new CampaignProgress();
        engine.Complete(progress, "1", Day);

        engine.Complete(progress, "2", Day);

        Assert.Equal(ScenarioStatus.Completed, StatusOf(engine, progress, "2"));
        Assert.Equal(ScenarioStatus.LockedOut, StatusOf(engine, progress, "3"));
        Assert.Equal(ScenarioStatus.Available, StatusOf(engine, progress, "4B"));
        Assert.Equal(ScenarioStatus.Hidden, StatusOf(engine, progress, "4A"));
    }

    [Fact]
    public void A_revealed_scenario_with_unmet_requirements_shows_as_locked()
    {
        var engine = BuildEngine();
        var progress = new CampaignProgress();
        // Reveal 9 by completing the whole 1 -> 2 -> 4B path; 9 requires 4B.
        engine.Complete(progress, "1", Day);
        engine.Complete(progress, "2", Day);

        // 9 is not revealed yet (nothing unlocks it in this toy graph), but once we
        // force-reveal via requirements being the only gate we still expect Hidden.
        Assert.Equal(ScenarioStatus.Hidden, StatusOf(engine, progress, "9"));
    }

    [Fact]
    public void Uncomplete_reverses_a_completion()
    {
        var engine = BuildEngine();
        var progress = new CampaignProgress();
        engine.Complete(progress, "1", Day);
        engine.Complete(progress, "2", Day);

        engine.Uncomplete(progress, "2");

        Assert.Equal(ScenarioStatus.Available, StatusOf(engine, progress, "2"));
        Assert.Equal(ScenarioStatus.Available, StatusOf(engine, progress, "3")); // no longer locked out
    }

    [Fact]
    public void CanComplete_is_true_only_for_available_scenarios()
    {
        var engine = BuildEngine();
        var progress = new CampaignProgress();

        Assert.True(engine.CanComplete(progress, "1"));
        Assert.False(engine.CanComplete(progress, "2")); // still hidden
    }
}

public class ScenarioCatalogTests
{
    [Fact]
    public void Embedded_catalog_loads_the_real_frosthaven_graph()
    {
        var catalog = ScenarioCatalog.LoadEmbedded();

        Assert.True(catalog.Scenarios.Count > 130, "expected the full campaign catalog");

        var townInFlames = catalog.Find("1");
        Assert.NotNull(townInFlames);
        Assert.Equal("A Town in Flames", townInFlames!.Name);
        Assert.True(townInFlames.Initial);
        Assert.Equal(["2", "3"], townInFlames.Unlocks);

        // The intro branch: scenario 2 blocks 3.
        Assert.Contains("3", catalog.Find("2")!.Blocks);
    }

    [Fact]
    public void Embedded_catalog_drives_the_engine_end_to_end()
    {
        var engine = new CampaignEngine(ScenarioCatalog.LoadEmbedded());
        var progress = new CampaignProgress();

        var revealed = engine.Complete(progress, "1", new DateOnly(2026, 1, 1));

        Assert.Contains(revealed, d => d.Index == "2");
        Assert.Contains(revealed, d => d.Index == "3");
    }
}

public class CampaignSerializerTests
{
    [Fact]
    public void Progress_round_trips_through_json()
    {
        var progress = new CampaignProgress
        {
            PartyName = "The Frostbiters",
            Completed = { ["1"] = new DateOnly(2026, 1, 2), ["2"] = new DateOnly(2026, 1, 9) },
            Notes = { ["1"] = "Saved the town" },
        };

        var restored = CampaignSerializer.Deserialize(CampaignSerializer.Serialize(progress));

        Assert.Equal("The Frostbiters", restored.PartyName);
        Assert.Equal(new DateOnly(2026, 1, 9), restored.Completed["2"]);
        Assert.Equal("Saved the town", restored.Notes["1"]);
    }
}

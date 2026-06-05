using FrosthavenCompanion.Domain;

namespace FrosthavenCompanion.Domain.Tests;

public class CampaignStatusTests
{
    [Fact]
    public void A_fresh_status_starts_on_week_one_at_prosperity_one()
    {
        var status = new CampaignStatus();

        Assert.Equal(1, status.Week);
        Assert.Equal(1, status.Prosperity);
        Assert.Empty(status.Resources);
    }

    [Fact]
    public void Status_round_trips_through_serialization()
    {
        var progress = new CampaignProgress();
        progress.Status.Week = 7;
        progress.Status.Prosperity = 3;
        progress.Status.Reputation = -4;
        progress.Status.Resources["Lumber"] = 5;

        var restored = CampaignSerializer.Deserialize(CampaignSerializer.Serialize(progress));

        Assert.Equal(7, restored.Status.Week);
        Assert.Equal(3, restored.Status.Prosperity);
        Assert.Equal(-4, restored.Status.Reputation);
        Assert.Equal(5, restored.Status.Resources["Lumber"]);
    }

    [Fact]
    public void Saves_written_before_status_existed_load_with_a_default_status()
    {
        // A save serialized when CampaignProgress had no Status property.
        const string legacy = """{"partyName":"Old Party","completed":{},"manualUnlocks":{},"notes":{}}""";

        var progress = CampaignSerializer.Deserialize(legacy);

        Assert.Equal("Old Party", progress.PartyName);
        Assert.NotNull(progress.Status);
        Assert.Equal(1, progress.Status.Week);
        Assert.Equal(1, progress.Status.Prosperity);
    }

    [Fact]
    public void Resource_names_cover_the_three_materials_and_six_herbs()
    {
        Assert.Equal(9, CampaignStatus.ResourceNames.Count);
        Assert.Contains("Lumber", CampaignStatus.ResourceNames);
        Assert.Contains("Snowthistle", CampaignStatus.ResourceNames);
    }
}

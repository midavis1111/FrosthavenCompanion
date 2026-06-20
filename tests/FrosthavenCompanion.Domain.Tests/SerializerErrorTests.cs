using FrosthavenCompanion.Domain;

namespace FrosthavenCompanion.Domain.Tests;

public class SerializerErrorTests
{
    [Theory]
    [InlineData("not json at all")]
    [InlineData("{ \"partyName\": ")]   // truncated
    [InlineData("[1, 2, 3]")]            // wrong shape
    public void Deserialize_throws_FormatException_for_malformed_json(string bad)
    {
        Assert.Throws<FormatException>(() => CampaignSerializer.Deserialize(bad));
    }

    [Fact]
    public void Deserialize_throws_FormatException_for_json_null()
    {
        Assert.Throws<FormatException>(() => CampaignSerializer.Deserialize("null"));
    }

    [Fact]
    public void Valid_progress_still_round_trips()
    {
        var progress = new CampaignProgress { PartyName = "Test" };
        var restored = CampaignSerializer.Deserialize(CampaignSerializer.Serialize(progress));
        Assert.Equal("Test", restored.PartyName);
    }
}

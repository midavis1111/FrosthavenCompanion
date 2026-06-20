using FrosthavenCompanion.Domain;

namespace FrosthavenCompanion.Domain.Tests;

public class CardCatalogTests
{
    [Fact]
    public void Blinkblade_cards_load_with_hand_size()
    {
        var catalog = CardCatalog.LoadEmbedded();

        Assert.True(catalog.Has("Blinkblade"));
        Assert.Equal(10, catalog.HandSize("Blinkblade"));

        var cards = catalog.For("Blinkblade");
        Assert.Equal(29, cards.Count);
        Assert.Contains(cards, c => c.Name == "Blurry Jab" && c.Level == "1");
        Assert.Contains(cards, c => c.Level == "X");
        Assert.All(cards, c => Assert.True(c.Id > 0));
    }

    [Fact]
    public void Unknown_class_has_no_cards()
    {
        var catalog = CardCatalog.LoadEmbedded();
        Assert.False(catalog.Has("Not A Class"));
        Assert.Empty(catalog.For("Not A Class"));
        Assert.Equal(0, catalog.HandSize("Not A Class"));
    }
}

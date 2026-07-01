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
        // Every Blinkblade card maps to a card-art image.
        Assert.All(cards, c => Assert.False(string.IsNullOrEmpty(c.Image)));
    }

    [Theory]
    [InlineData(2050, "20/50")]  // Blinkblade fast/slow pair
    [InlineData(232, "2/32")]    // single-digit fast (leading zero dropped)
    [InlineData(50, "50")]       // a normal 2-digit initiative
    public void InitiativeDisplay_splits_fast_slow_pairs(int initiative, string expected) =>
        Assert.Equal(expected, new AbilityCard { Id = 1, Name = "x", Initiative = initiative }.InitiativeDisplay);

    [Theory]
    [InlineData("Deathwalker")]
    [InlineData("Geminate")]
    [InlineData("Trapper")]
    [InlineData("Pyroclast")]
    public void Authored_classes_load_cards_with_images(string className)
    {
        var catalog = CardCatalog.LoadEmbedded();
        var cards = catalog.For(className);

        Assert.NotEmpty(cards);
        Assert.True(catalog.HandSize(className) > 0);
        Assert.All(cards, c => Assert.False(string.IsNullOrEmpty(c.Image)));
    }

    [Fact]
    public void Catalog_lists_all_authored_classes()
    {
        var catalog = CardCatalog.LoadEmbedded();
        Assert.Superset(
            new HashSet<string> { "blinkblade", "deathwalker", "geminate", "trapper", "pyroclast" },
            catalog.Classes.ToHashSet());
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

using FrosthavenCompanion.Domain;

namespace FrosthavenCompanion.Domain.Tests;

public class PerkCatalogTests
{
    [Fact]
    public void Blinkblade_perks_load_with_checkbox_counts()
    {
        var catalog = PerkCatalog.LoadEmbedded();

        Assert.True(catalog.Has("Blinkblade"));
        var perks = catalog.For("Blinkblade");
        Assert.Equal(11, perks.Count);
        Assert.All(perks, p => Assert.True(p.Boxes is >= 1 and <= 3));
        Assert.Contains(perks, p => p.Text.Contains("Remove one -2"));
    }

    [Fact]
    public void For_accepts_display_name_or_slug_and_is_empty_for_unknown()
    {
        var catalog = PerkCatalog.LoadEmbedded();
        Assert.NotEmpty(catalog.For("Blinkblade"));
        Assert.NotEmpty(catalog.For("blinkblade"));
        Assert.Empty(catalog.For("Not A Class"));
    }

    [Theory]
    [InlineData("Banner Spear", "banner-spear")]
    [InlineData("  Blinkblade ", "blinkblade")]
    public void Slugify_normalizes_class_names(string input, string expected) =>
        Assert.Equal(expected, PerkCatalog.Slugify(input));
}

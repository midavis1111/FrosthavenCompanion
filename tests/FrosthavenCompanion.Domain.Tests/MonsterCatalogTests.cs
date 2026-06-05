using FrosthavenCompanion.Domain;

namespace FrosthavenCompanion.Domain.Tests;

public class MonsterCatalogTests
{
    [Fact]
    public void Embedded_catalog_loads_monsters_with_per_level_stats()
    {
        var catalog = MonsterCatalog.LoadEmbedded();

        Assert.True(catalog.Monsters.Count > 100, "expected the full monster set");

        var archer = catalog.Find("algox-archer");
        Assert.NotNull(archer);
        Assert.Equal("Algox Archer", archer!.DisplayName);

        var l7Elite = archer.Levels.Single(l => l.Level == 7).Elite;
        Assert.NotNull(l7Elite);
        Assert.Equal("33", l7Elite!.Health);
        Assert.Equal("7", l7Elite.Attack);
    }

    [Fact]
    public void Find_works_by_slug_and_display_name()
    {
        var catalog = MonsterCatalog.LoadEmbedded();
        Assert.NotNull(catalog.Find("black-imp"));
        Assert.NotNull(catalog.Find("Black Imp"));
        Assert.Null(catalog.Find("not-a-monster"));
    }

    [Fact]
    public void Monster_abilities_and_conditions_are_extracted()
    {
        var catalog = MonsterCatalog.LoadEmbedded();
        var imp = catalog.Find("black-imp")!;
        var l1 = imp.Levels.Single(l => l.Level == 1).Normal!;
        Assert.Contains("poison", l1.Conditions);
    }
}

public class ConditionsTests
{
    [Fact]
    public void Glossary_has_the_common_conditions_and_lookup_is_case_insensitive()
    {
        Assert.True(Conditions.All.Count >= 15);
        Assert.NotNull(Conditions.Find("poison"));
        Assert.NotNull(Conditions.Find("Strengthen"));
        Assert.True(Conditions.Find("Bless")!.Positive);
        Assert.False(Conditions.Find("Wound")!.Positive);
        Assert.Null(Conditions.Find("notacondition"));
    }
}

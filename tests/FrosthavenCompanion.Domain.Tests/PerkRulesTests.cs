using FrosthavenCompanion.Domain;

namespace FrosthavenCompanion.Domain.Tests;

public class PerkRulesTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(9, 8)]
    public void FromLeveling_is_one_per_level_after_the_first(int level, int expected) =>
        Assert.Equal(expected, PerkRules.FromLeveling(level));

    [Fact]
    public void Budget_counts_earned_from_level_plus_extra_and_flags_over()
    {
        // Level 5 → 4 from leveling, +2 extra = 6 earned; 7 assigned → over by 1.
        var b = PerkRules.Budget(level: 5, extra: 2, assigned: 7, bonus: 1);

        Assert.Equal(6, b.Earned);
        Assert.Equal(7, b.Assigned);
        Assert.Equal(1, b.Bonus);
        Assert.Equal(-1, b.Remaining);
        Assert.Equal(PerkBudgetState.Over, b.State);
    }

    [Fact]
    public void Budget_reports_under_and_exact()
    {
        Assert.Equal(PerkBudgetState.Under, PerkRules.Budget(3, 0, 1, 0).State); // earned 2, assigned 1
        Assert.Equal(PerkBudgetState.Exact, PerkRules.Budget(3, 0, 2, 0).State); // earned 2, assigned 2
    }

    [Fact]
    public void Bonus_perks_do_not_count_against_the_earned_total()
    {
        // earned 2, assigned 2 (exact) even though there are 3 bonus perks on top.
        var b = PerkRules.Budget(level: 3, extra: 0, assigned: 2, bonus: 3);
        Assert.Equal(PerkBudgetState.Exact, b.State);
    }
}

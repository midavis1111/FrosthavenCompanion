namespace FrosthavenCompanion.Domain;

/// <summary>The outcome of checking how many perks are assigned vs earned.</summary>
public enum PerkBudgetState { Under, Exact, Over }

/// <summary>A perk-count check: how many a character has earned, assigned, and whether that's valid.</summary>
public readonly record struct PerkBudget(int Earned, int Assigned, int Bonus)
{
    /// <summary>Earned minus assigned: positive = unspent, negative = over-assigned.</summary>
    public int Remaining => Earned - Assigned;

    public PerkBudgetState State => Remaining switch
    {
        > 0 => PerkBudgetState.Under,
        < 0 => PerkBudgetState.Over,
        _ => PerkBudgetState.Exact,
    };
}

/// <summary>Frosthaven perk-count rules.</summary>
public static class PerkRules
{
    /// <summary>Perks gained from leveling: one per level after the first.</summary>
    public static int FromLeveling(int level) => Math.Max(0, level - 1);

    /// <summary>
    /// Builds a perk budget. <paramref name="extra"/> covers perks earned outside
    /// leveling (battle goals, masteries, …); <paramref name="bonus"/> are the
    /// extra-card perks, counted separately (not against the earned total).
    /// </summary>
    public static PerkBudget Budget(int level, int extra, int assigned, int bonus) =>
        new(FromLeveling(level) + Math.Max(0, extra), assigned, bonus);
}

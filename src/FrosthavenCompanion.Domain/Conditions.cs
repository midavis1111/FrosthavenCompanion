namespace FrosthavenCompanion.Domain;

/// <summary>A status condition and a short description of what it does.</summary>
public sealed record Condition(string Name, string Description, bool Positive);

/// <summary>
/// The standard Frosthaven status conditions, with concise original descriptions
/// (game rules, paraphrased). Used by the conditions glossary and for tooltips on
/// monster stats. Lookup is case-insensitive by name.
/// </summary>
public static class Conditions
{
    public static readonly IReadOnlyList<Condition> All =
    [
        // Negative
        new("Poison", "While poisoned, the figure suffers +1 damage from each attack against it. Removed by any heal.", false),
        new("Wound", "At the start of each of its turns, the figure suffers 1 damage. Removed by any heal.", false),
        new("Bane", "The figure suffers 10 damage at the end of its next turn, then bane is removed. Also removed by any heal.", false),
        new("Brittle", "The next time the figure suffers damage, it suffers double. Then brittle is removed (also removed by a heal).", false),
        new("Immobilize", "The figure cannot move on its turn (it may still attack). Removed at the end of its next turn.", false),
        new("Disarm", "The figure cannot attack on its turn (it may still move). Removed at the end of its next turn.", false),
        new("Stun", "The figure cannot move, attack, or use abilities on its turn. Removed at the end of its next turn.", false),
        new("Muddle", "The figure's attacks gain Disadvantage. Removed at the end of its next turn.", false),
        new("Curse", "Shuffle a null (×0) attack modifier card into the figure's deck; it is removed when drawn.", false),
        new("Impair", "Characters only: the figure cannot use or trigger items (bonuses already applied remain). Removed at the end of its next turn.", false),

        // Positive
        new("Bless", "Shuffle a ×2 attack modifier card into the figure's deck; it is removed when drawn.", true),
        new("Strengthen", "The figure's attacks gain Advantage. Removed at the end of its next turn.", true),
        new("Ward", "The next time the figure suffers damage, it suffers half (rounded down). Then ward is removed.", true),
        new("Invisible", "The figure cannot be targeted or focused by enemies. Removed at the end of its next turn.", true),
        new("Regenerate", "At the start of each of its turns, the figure recovers 1 hit point. Removed at the end of its next turn.", true),
    ];

    private static readonly Dictionary<string, Condition> ByName =
        All.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>Finds a condition by name (case-insensitive), or null.</summary>
    public static Condition? Find(string name) => ByName.GetValueOrDefault(name.Trim());
}

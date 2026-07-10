namespace FrosthavenCompanion.App.Services;

/// <summary>
/// Maps an authored card effect tag (the player's own vocabulary) to an icon and
/// tooltip, reusing the existing condition/game icons and a small set of extra
/// tag icons. Returns a null icon for tags we have no art for (render as text).
/// </summary>
public static class TagIcons
{
    private static readonly Dictionary<string, (string? Icon, string Tip)> Map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Standard conditions under the player's names.
            ["Bleed"] = ("icons/conditions/wound.png", "Bleed (Wound)"),
            ["Wound"] = ("icons/conditions/wound.png", "Wound"),
            ["Pin"] = ("icons/conditions/immobilize.png", "Pin (Immobilize)"),
            ["Immobilize"] = ("icons/conditions/immobilize.png", "Immobilize"),
            ["Vanish"] = ("icons/conditions/invisible.png", "Vanish (Invisible)"),
            ["Muddle"] = ("icons/conditions/muddle.png", "Muddle"),
            ["Stun"] = ("icons/conditions/stun.png", "Stun"),
            ["Bless"] = ("icons/conditions/bless.png", "Bless"),
            ["Curse"] = ("icons/conditions/curse.png", "Curse"),
            ["Bane"] = ("icons/conditions/bane.png", "Bane"),
            ["Brittle"] = ("icons/conditions/brittle.png", "Brittle"),
            ["Disarm"] = ("icons/conditions/disarm.png", "Disarm"),
            ["Impair"] = ("icons/conditions/impair.png", "Impair"),
            ["Poison"] = ("icons/conditions/poison.png", "Poison"),
            ["Ward"] = ("icons/conditions/ward.png", "Ward"),
            ["Regenerate"] = ("icons/conditions/regenerate.png", "Regenerate"),
            ["Strengthen"] = ("icons/conditions/strengthen.png", "Strengthen"),

            // Game symbols we already have.
            ["Move"] = ("icons/game/move.png", "Move"),
            ["Time Token"] = ("icons/game/time.png", "Time token"),

            // Extra tag icons (from worldhaven).
            ["XP"] = ("icons/tags/xp.png", "Experience"),
            ["Heal"] = ("icons/tags/heal.png", "Heal"),
            ["Self Heal"] = ("icons/tags/heal.png", "Self heal"),
            ["Loot"] = ("icons/tags/loot.png", "Loot"),
            ["Jump"] = ("icons/tags/jump.png", "Jump"),
            ["Shield"] = ("icons/tags/shield.png", "Shield"),
            ["Damage"] = ("icons/tags/damage.png", "Damage"),
            ["Summon"] = ("icons/tags/summon.png", "Summon"),
            ["Push"] = ("icons/tags/push.png", "Push"),
            ["Pull"] = ("icons/tags/pull.png", "Pull"),
            ["Pierce"] = ("icons/tags/pierce.png", "Pierce"),
            ["Fire"] = ("icons/tags/fire.png", "Fire"),
            ["Water"] = ("icons/tags/ice.png", "Water / Ice"),
            ["Ice"] = ("icons/tags/ice.png", "Ice"),
            ["Air"] = ("icons/tags/air.png", "Air"),
            ["Earth"] = ("icons/tags/earth.png", "Earth"),
        };

    /// <summary>Resolves a tag to its icon path (or null) and tooltip.</summary>
    public static (string? Icon, string Tip) Resolve(string tag) =>
        Map.TryGetValue(tag, out var v) ? v : (null, tag);
}

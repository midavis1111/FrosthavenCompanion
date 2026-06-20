using FrosthavenCompanion.Domain;

namespace FrosthavenCompanion.App.Services;

/// <summary>
/// Resolves an icon token slug (e.g. "wound", "move", "spent") to its image path
/// and a tooltip. Condition slugs reuse the conditions icon set + descriptions;
/// other game symbols come from wwwroot/icons/game. Used by the IconText component
/// to render Frosthaven symbols inline in perk/mastery text.
/// </summary>
public static class GameIcons
{
    // Non-condition game symbols: slug -> (file in icons/game, tooltip).
    private static readonly Dictionary<string, (string File, string Tip)> Game = new(StringComparer.OrdinalIgnoreCase)
    {
        ["move"] = ("move", "Move"),
        ["attack"] = ("attack", "Attack"),
        ["spent"] = ("spent", "Spent — flip the item to its spent side"),
        ["recover"] = ("recover", "Recover — refresh a spent item"),
        ["rolling"] = ("rolling", "Rolling modifier — apply it and draw another attack modifier card"),
        ["time"] = ("time", "Time token (Blinkblade)"),
    };

    /// <summary>Returns the image src and tooltip for a token slug, or null if unknown.</summary>
    public static (string Src, string Tooltip)? Resolve(string slug)
    {
        if (Conditions.Find(slug) is { } c)
            return ($"icons/conditions/{c.Icon}.png", $"{c.Name}: {c.Description}");
        if (Game.TryGetValue(slug, out var g))
            return ($"icons/game/{g.File}.png", g.Tip);
        return null;
    }
}

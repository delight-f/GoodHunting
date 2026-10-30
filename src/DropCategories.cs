namespace GoodHunting;

/// <summary>Drop category derived from an item code, used to pick a multiplier.</summary>
public enum DropCategory
{
    None,
    Hide,
    Fat,
    Meat,
    Feather,
}

/// <summary>
/// Maps a drop's item code to a <see cref="DropCategory"/>.
/// Pure logic -- no game assembly dependency, so it is unit-testable on its own.
/// </summary>
public static class DropCategories
{
    public static DropCategory Classify(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return DropCategory.None;
        }

        // Strip an optional "domain:" prefix (e.g. "game:fish-raw").
        var path = code;
        var colon = code.IndexOf(':');
        if (colon >= 0)
        {
            path = code[(colon + 1)..];
        }

        if (path.StartsWith("hide-raw", System.StringComparison.Ordinal))
        {
            return DropCategory.Hide;
        }

        // Some animal mods (e.g. Fauna of the Stone Age) drop a species pelt instead of a vanilla
        // hide; it is the same thing to the player and converts to hide via a recipe.
        if (path.StartsWith("pelt", System.StringComparison.Ordinal))
        {
            return DropCategory.Hide;
        }

        if (path == "fat" || path.StartsWith("fat-", System.StringComparison.Ordinal))
        {
            return DropCategory.Fat;
        }

        switch (path)
        {
            case "redmeat-raw":
            case "bushmeat-raw":
            case "fish-raw":
            case "poultry-raw":
                return DropCategory.Meat;
        }

        if (path == "feather" || path.StartsWith("feather-", System.StringComparison.Ordinal))
        {
            return DropCategory.Feather;
        }

        return DropCategory.None;
    }
}

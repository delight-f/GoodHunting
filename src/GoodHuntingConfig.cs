using System;
using Newtonsoft.Json;

namespace GoodHunting;

/// <summary>
/// User-configurable drop multipliers, persisted to <c>ModConfig/goodhuntingforked.json</c>.
/// A multiplier of 1.0 is the identity; values are clamped to [0, 100].
/// </summary>
public sealed class GoodHuntingConfig
{
    public const float MinMultiplier = 0f;
    public const float MaxMultiplier = 100f;
    public const float Identity = 1f;

    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonProperty("hideMultiplier")]
    public float HideMultiplier { get; set; } = Identity;

    [JsonProperty("fatMultiplier")]
    public float FatMultiplier { get; set; } = Identity;

    [JsonProperty("meatMultiplier")]
    public float MeatMultiplier { get; set; } = Identity;

    [JsonProperty("featherMultiplier")]
    public float FeatherMultiplier { get; set; } = Identity;

    /// <summary>Applied last, on top of the per-category multiplier, to every drop.</summary>
    [JsonProperty("allMultiplier")]
    public float AllMultiplier { get; set; } = Identity;

    /// <summary>
    /// Domains of other mods' animals whose drops the multipliers also apply to. Empty leaves
    /// modded animals exactly as they are; <c>"*"</c> covers every modded domain. The vanilla
    /// domain is always excluded -- its animals come from the tables and would scale twice.
    /// </summary>
    [JsonProperty("moddedAnimalDomains")]
    public string[] ModdedAnimalDomains { get; set; } = Array.Empty<string>();

    private const string VanillaDomain = "game";

    /// <summary>Whether an entity in this domain has its drops scaled by this mod.</summary>
    public bool ScalesDomain(string? domain)
    {
        if (!Enabled || string.IsNullOrEmpty(domain)
            || domain.Equals(VanillaDomain, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return ModdedAnimalDomains?.Any(entry =>
            entry == "*" || string.Equals(entry, domain, StringComparison.OrdinalIgnoreCase)) == true;
    }

    public float ForCategory(DropCategory category) => category switch
    {
        DropCategory.Hide => HideMultiplier,
        DropCategory.Fat => FatMultiplier,
        DropCategory.Meat => MeatMultiplier,
        DropCategory.Feather => FeatherMultiplier,
        _ => Identity,
    };

    /// <summary>Combined multiplier for a drop code: category multiplier times the global one, clamped.</summary>
    public float Effective(string? itemCode)
    {
        if (!Enabled)
        {
            return Identity;
        }

        return Clamp(ForCategory(DropCategories.Classify(itemCode)) * AllMultiplier);
    }

    public static float Clamp(float multiplier)
    {
        if (float.IsNaN(multiplier))
        {
            return Identity;
        }

        return Math.Clamp(multiplier, MinMultiplier, MaxMultiplier);
    }

    /// <summary>Clamp every multiplier into range. Returns true if anything changed.</summary>
    public bool ClampAll()
    {
        var changed = false;

        float ClampField(float value)
        {
            var clamped = Clamp(value);
            if (!clamped.Equals(value))
            {
                changed = true;
            }
            return clamped;
        }

        HideMultiplier = ClampField(HideMultiplier);
        FatMultiplier = ClampField(FatMultiplier);
        MeatMultiplier = ClampField(MeatMultiplier);
        FeatherMultiplier = ClampField(FeatherMultiplier);
        AllMultiplier = ClampField(AllMultiplier);
        return changed;
    }
}

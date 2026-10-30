using System;
using System.Collections.Generic;
using System.IO;
using GoodHunting.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace GoodHunting;

/// <summary>
/// Owns animal harvest drops, animal health and fish processing yields, and applies the configured
/// multipliers. Server-only: every change reaches clients through the server's asset packet (entity
/// attributes and client behaviours, collectible behaviour properties), so clients need neither the
/// mod nor a matching config.
/// </summary>
public class GoodHuntingModSystem : ModSystem
{
    public const string ConfigFileName = "goodhuntingforked.json";

    private GoodHuntingConfig config = new();

    // First-seen drops for opted-in modded animals. In-place scaling is not idempotent, so a
    // reload must re-scale this baseline rather than the already-scaled behaviour.
    private readonly Dictionary<string, JArray> moddedBaseline = new(StringComparer.Ordinal);

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

    // Late in AssetsFinalize so these tables win over other mods' finalize-time edits. The built-in
    // harvestable drop sync (ExecuteOrder 0.1) has already copied the vanilla drops by then, so
    // ApplyEntities refreshes that copy itself.
    public override double ExecuteOrder() => 1.0;

    public override void StartPre(ICoreAPI api)
    {
        GoodHuntingConfig? loaded;
        try
        {
            loaded = api.LoadModConfig<GoodHuntingConfig>(ConfigFileName);
        }
        catch (Exception e) when (e is JsonException or IOException)
        {
            // Leave the user's file alone so they can fix it.
            Mod.Logger.Error("ModConfig/{0} could not be read and was left untouched; running with default settings until it is fixed: {1}",
                ConfigFileName, e.Message);
            return;
        }

        config = loaded ?? new GoodHuntingConfig();
        if (loaded == null || config.ClampAll())
        {
            api.StoreModConfig(config, ConfigFileName);
        }
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        if (!config.Enabled)
        {
            Mod.Logger.Notification("{0} disabled by config; vanilla drops and health left untouched.", Mod.Info.Version);
            return;
        }

        var entities = ApplyEntities(api);
        var fish = ApplyFish(api);
        ApplyModdedEntities(api);

        Mod.Logger.Notification("{0} applied to {1}/{2} entity types and {3}/{4} fish variants " +
            "(hide={5}, fat={6}, meat={7}, feather={8}, all={9}).",
            Mod.Info.Version, entities, GoodHuntingData.Entities.Count, fish, GoodHuntingData.Fish.Count,
            config.HideMultiplier, config.FatMultiplier, config.MeatMultiplier,
            config.FeatherMultiplier, config.AllMultiplier);
    }

    private int ApplyEntities(ICoreAPI api)
    {
        var applied = new HashSet<string>();
        foreach (var type in api.World.EntityTypes)
        {
            if (type?.Code?.Domain != GlobalConstants.DefaultDomain
                || !GoodHuntingData.Entities.TryGetValue(type.Code.Path, out var entry)
                || type.Server?.BehaviorsAsJsonObj is not { } behaviors)
            {
                continue;
            }

            var dropsDone = !entry.DropsChanged;
            var healthDone = !entry.HealthChanged;
            foreach (var behavior in behaviors)
            {
                if (behavior?.Token is not JObject token)
                {
                    continue;
                }

                switch (behavior["code"].AsString())
                {
                    // Write only the keys we own: sibling keys (sounds, hail damage, other mods'
                    // patches) must survive.
                    case "harvestable" when entry.DropsChanged:
                        var drops = DropJson.Drops(DropScaling.ScaleAll(entry.Drops, config));
                        token["drops"] = drops;
                        // Clients read the handbook's "obtained by killing" lists from this attribute.
                        if (type.Attributes?.Token is not JObject attributes)
                        {
                            attributes = new JObject();
                            type.Attributes = new JsonObject(attributes);
                        }
                        attributes["harvestableDrops"] = drops.DeepClone();
                        dropsDone = true;
                        break;

                    // The client's health behaviour shares this token, so it follows too.
                    case "health" when entry.HealthChanged:
                        token["maxhealth"] = entry.MaxHealth;
                        token["currenthealth"] = entry.CurrentHealth;
                        healthDone = true;
                        break;
                }
            }

            if (dropsDone && healthDone)
            {
                applied.Add(type.Code.Path);
            }
        }

        WarnUnapplied("entity types", GoodHuntingData.Entities.Keys, applied);
        return applied.Count;
    }

    private int ApplyFish(ICoreAPI api)
    {
        var applied = new HashSet<string>();
        foreach (var collectible in api.World.Collectibles)
        {
            if (collectible?.Code?.Domain != GlobalConstants.DefaultDomain
                || !GoodHuntingData.Fish.TryGetValue(collectible.Code.Path, out var table)
                || collectible.CollectibleBehaviors?.OfType<CollectibleBehaviorGroundStoredProcessable>().FirstOrDefault() is not { } behavior)
            {
                continue;
            }

            var drops = DropJson.Drops(DropScaling.ScaleAll(table, config));

            // Parsed exactly as the behaviour's own Initialize does; its OnLoaded (StartServerSide
            // phase, after AssetsFinalize) resolves the stacks.
            behavior.ProcessedStacks = new JsonObject(drops).AsObject<BlockDropItemStack[]>(null);

            // Clients rebuild the behaviour from this string (sent in the server asset packet, which
            // is built after AssetsFinalize), so the handbook shows the server's yields.
            var properties = JObject.Parse(behavior.propertiesAtString);
            properties["processedStacks"] = drops;
            behavior.propertiesAtString = properties.ToString(Formatting.None);

            applied.Add(collectible.Code.Path);
        }

        WarnUnapplied("fish variants", GoodHuntingData.Fish.Keys, applied);
        return applied.Count;
    }

    // Other mods' animals keep whatever their own mod resolved. When the user names their domains,
    // scale those drops in place from that resolved array -- nothing is copied out of the other mod,
    // so its next release can add species or retune values with no change here.
    private void ApplyModdedEntities(ICoreAPI api)
    {
        var configured = config.ModdedAnimalDomains;
        if (configured == null || configured.Length == 0)
        {
            return;
        }

        var scaled = 0;
        var withoutDrops = 0;
        var matchedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in api.World.EntityTypes)
        {
            if (type is null || type.Code is null)
            {
                continue;
            }

            var domain = type.Code.Domain;
            if (domain == GlobalConstants.DefaultDomain || !config.ScalesDomain(domain))
            {
                continue;
            }

            var behaviors = type.Server?.BehaviorsAsJsonObj;
            if (behaviors == null)
            {
                continue;
            }

            matchedDomains.Add(domain);

            JObject? harvest = null;
            foreach (var behavior in behaviors)
            {
                if (behavior?.Token is JObject token && token["code"]?.Value<string>() == "harvestable")
                {
                    harvest = token;
                    break;
                }
            }

            if (harvest?["drops"] is not JArray current)
            {
                // Carcass mods can replace the harvest behaviour outright; there is nothing to scale.
                withoutDrops++;
                continue;
            }

            var key = domain + ":" + type.Code.Path;
            if (!moddedBaseline.TryGetValue(key, out var baseline))
            {
                baseline = (JArray)current.DeepClone();
                moddedBaseline[key] = baseline;
            }

            var drops = DropScaling.ScaleExisting(baseline, config);
            if (JToken.DeepEquals(current, drops))
            {
                continue;
            }

            harvest["drops"] = drops;

            // Same handbook copy the vanilla path refreshes: the game synced it before this ran, so
            // it would otherwise show the other mod's unscaled numbers to players.
            if (type.Attributes?.Token is not JObject attributes)
            {
                attributes = new JObject();
                type.Attributes = new JsonObject(attributes);
            }
            attributes["harvestableDrops"] = drops.DeepClone();
            scaled++;
        }

        if (matchedDomains.Count == 0)
        {
            Mod.Logger.Warning("moddedAnimalDomains is set, but no entity types loaded for {0}; check the domain ids.",
                string.Join(", ", configured));
            return;
        }

        Mod.Logger.Notification("{0} scaled drops on {1} modded entity types across {2} domain(s): {3}.",
            Mod.Info.Version, scaled, matchedDomains.Count, string.Join(", ", matchedDomains));

        if (withoutDrops > 0)
        {
            Mod.Logger.Warning("{0} modded entity types have no harvestable drops to scale and keep their own values.", withoutDrops);
        }

        if (!configured.Contains("*"))
        {
            var missing = configured
                .Where(entry => !string.IsNullOrEmpty(entry) && !matchedDomains.Contains(entry))
                .ToList();
            if (missing.Count > 0)
            {
                Mod.Logger.Warning("moddedAnimalDomains lists domains with no loaded entity types: {0}.",
                    string.Join(", ", missing));
            }
        }
    }

    // A game update that renames an entity, item or behaviour turns its table entry into a no-op;
    // say so instead of failing silently.
    private void WarnUnapplied(string kind, IEnumerable<string> owned, HashSet<string> applied)
    {
        var missing = owned.Where(code => !applied.Contains(code)).ToList();
        if (missing.Count > 0)
        {
            Mod.Logger.Warning("{0} {1} could not be updated (not in the game, or missing the expected behaviour): {2}",
                missing.Count, kind, string.Join(", ", missing));
        }
    }
}

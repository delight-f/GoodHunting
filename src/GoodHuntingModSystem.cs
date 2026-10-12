using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GoodHunting.Data;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace GoodHunting;

/// <summary>
/// Good Hunting 2.0.0. Owns animal harvest drops + health in C# and applies configurable
/// multipliers. Injects by rewriting <see cref="EntityServerProperties.BehaviorsAsJsonObj"/>
/// (the same structure the legacy content-patch mod produced) and by setting the fish
/// processing behaviour's <see cref="CollectibleBehaviorGroundStoredProcessable.ProcessedStacks"/>.
/// </summary>
public class GoodHuntingModSystem : ModSystem
{
    public const string ConfigFileName = "goodhunting.json";

    private GoodHuntingConfig config = new();
    private int entitiesApplied;
    private int fishApplied;

    public override bool ShouldLoad(EnumAppSide forSide) => true;

    // Run after the built-in harvestable drop sync (ExecuteOrder 0.1).
    public override double ExecuteOrder() => 1.0;

    public override void StartPre(ICoreAPI api)
    {
        var loaded = api.LoadModConfig<GoodHuntingConfig>(ConfigFileName);
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
            api.Logger.Notification("[goodhunting] disabled by config; vanilla drops left untouched.");
            return;
        }

        ApplyEntities(api);
        ApplyFish(api);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        if (!config.Enabled)
        {
            api.Logger.Notification("[goodhunting] 2.0.0 loaded but disabled by config; vanilla drops in effect.");
            return;
        }

        api.Logger.Notification("[goodhunting] applied drops to {0} entity types and {1} fish variants " +
            "(hide={2}, fat={3}, meat={4}, feather={5}, all={6}).",
            entitiesApplied, fishApplied,
            config.HideMultiplier, config.FatMultiplier, config.MeatMultiplier,
            config.FeatherMultiplier, config.AllMultiplier);
    }

    private void ApplyEntities(ICoreAPI api)
    {
        foreach (var type in api.World.EntityTypes)
        {
            if (type?.Code == null || !GoodHuntingData.Entities.TryGetValue(type.Code.Path, out var entry))
            {
                continue;
            }

            var behaviors = type.Server?.BehaviorsAsJsonObj;
            if (behaviors == null)
            {
                continue;
            }

            for (var i = 0; i < behaviors.Length; i++)
            {
                var behavior = behaviors[i];
                var code = behavior?["code"]?.AsString();

                if (code == "harvestable" && entry.DropsChanged && behavior != null && behavior.Token is JObject obj)
                {
                    obj["drops"] = JArray.Parse(BuildDropsJson(DropScaling.ScaleAll(entry.Drops, config)));
                }
                else if (code == "health" && entry.HealthChanged)
                {
                    behaviors[i] = JsonObject.FromJson(BuildHealthJson(entry));
                }
            }

            entitiesApplied++;
        }
    }

    private void ApplyFish(ICoreAPI api)
    {
        foreach (var collectible in api.World.Collectibles)
        {
            if (collectible?.Code == null || !GoodHuntingData.Fish.TryGetValue(collectible.Code.Path, out var table))
            {
                continue;
            }

            var behavior = FindProcessable(collectible);
            if (behavior == null)
            {
                continue;
            }

            // The engine resolved the *vanilla* stacks during OnLoaded, before AssetsFinalize.
            // We replace them, so we must resolve the new stacks ourselves or they are unusable
            // (BlockDropItemStack.GetNextItemStack returns null while ResolvedItemstack is null).
            var stacks = BuildStacks(DropScaling.ScaleAll(table, config));
            foreach (var stack in stacks)
            {
                stack.Resolve(api.World, "goodhunting", collectible.Code);
            }

            behavior.ProcessedStacks = stacks;
            fishApplied++;
        }
    }

    private static CollectibleBehaviorGroundStoredProcessable? FindProcessable(CollectibleObject collectible)
    {
        if (collectible.CollectibleBehaviors == null)
        {
            return null;
        }

        foreach (var behavior in collectible.CollectibleBehaviors)
        {
            if (behavior is CollectibleBehaviorGroundStoredProcessable processable)
            {
                return processable;
            }
        }

        return null;
    }

    private static BlockDropItemStack[] BuildStacks(IReadOnlyList<DropSpec> drops)
    {
        var stacks = new BlockDropItemStack[drops.Count];
        for (var i = 0; i < drops.Count; i++)
        {
            var drop = drops[i];
            stacks[i] = new BlockDropItemStack
            {
                Type = string.Equals(drop.Type, "block", System.StringComparison.OrdinalIgnoreCase)
                    ? EnumItemClass.Block
                    : EnumItemClass.Item,
                Code = new AssetLocation(drop.Code),
                Quantity = new NatFloat(drop.Avg, drop.Var, EnumDistribution.UNIFORM),
            };
        }
        return stacks;
    }

    private static string BuildDropsJson(IReadOnlyList<DropSpec> drops)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        for (var i = 0; i < drops.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            var d = drops[i];
            sb.Append("{\"type\":").Append(Quote(d.Type))
              .Append(",\"code\":").Append(Quote(d.Code))
              .Append(",\"quantity\":{\"avg\":").Append(F(d.Avg))
              .Append(",\"var\":").Append(F(d.Var)).Append("}}");
        }
        sb.Append(']');
        return sb.ToString();
    }

    private static string BuildHealthJson(EntityEntry entry)
    {
        return "{\"code\":\"health\",\"currenthealth\":" + F(entry.CurrentHealth)
            + ",\"maxhealth\":" + F(entry.MaxHealth) + "}";
    }

    private static string F(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string Quote(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (var c in value)
        {
            sb.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                _ => c.ToString(),
            });
        }
        sb.Append('"');
        return sb.ToString();
    }
}

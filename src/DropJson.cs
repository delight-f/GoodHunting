using System.Collections.Generic;
using GoodHunting.Data;
using Newtonsoft.Json.Linq;

namespace GoodHunting;

/// <summary>
/// Builds the behaviour objects we inject into the engine. Pure construction with no game
/// assembly dependency, so the exact shape and number formatting can be unit-tested.
/// </summary>
public static class DropJson
{
    public static JArray Drops(IReadOnlyList<DropSpec> drops)
    {
        var array = new JArray();
        foreach (var drop in drops)
        {
            array.Add(new JObject
            {
                ["type"] = drop.Type,
                ["code"] = drop.Code,
                ["quantity"] = new JObject
                {
                    ["avg"] = drop.Avg,
                    ["var"] = drop.Var,
                },
            });
        }
        return array;
    }

    public static JObject Health(EntityEntry entry) => new()
    {
        ["code"] = "health",
        ["currenthealth"] = entry.CurrentHealth,
        ["maxhealth"] = entry.MaxHealth,
    };
}

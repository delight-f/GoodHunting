using System.Collections.Generic;
using GoodHunting.Data;
using Newtonsoft.Json.Linq;

namespace GoodHunting;

/// <summary>
/// Applies multipliers to <see cref="DropSpec"/> tables. Pure logic -- no game assembly
/// dependency. Never mutates the source table; always returns new instances, so the
/// static golden data stays pristine across reloads and across sides.
/// </summary>
public static class DropScaling
{
    public static DropSpec Scale(DropSpec source, GoodHuntingConfig config)
    {
        var m = config.Effective(source.Code);
        return new DropSpec(source.Type, source.Code, source.Avg * m, source.Var * m);
    }

    public static DropSpec[] ScaleAll(IReadOnlyList<DropSpec> source, GoodHuntingConfig config)
    {
        var result = new DropSpec[source.Count];
        for (var i = 0; i < source.Count; i++)
        {
            result[i] = Scale(source[i], config);
        }
        return result;
    }

    // The quantity shapes the engine accepts. avg/var is what 1.22 uses; min/max are tolerated
    // so a mod that writes them is scaled rather than silently skipped. offset is a flat addition
    // on top of the roll -- a pelt written as {offset:1, avg:0, var:0} drops exactly one, so
    // leaving offset alone would make the hide multiplier do nothing to it.
    private static readonly string[] QuantityKeys = { "avg", "var", "min", "max", "offset" };

    /// <summary>
    /// Scales a drops array already resolved by the engine for another mod's entity. Working on
    /// the JSON the engine loaded means no values are copied out of that mod, so a release that
    /// retunes its animals needs no change here. Returns a new array and never mutates the input:
    /// the caller keeps the original as its baseline. Entries whose quantity is not a plain
    /// numeric object are passed through untouched rather than guessed at.
    /// </summary>
    public static JArray ScaleExisting(JArray source, GoodHuntingConfig config)
    {
        var result = new JArray();
        foreach (var item in source)
        {
            if (item is not JObject entry
                || entry.Value<string>("code") is not { } code
                || entry["quantity"] is not JObject quantity)
            {
                result.Add(item.DeepClone());
                continue;
            }

            var m = config.Effective(code);
            if (m.Equals(GoodHuntingConfig.Identity))
            {
                result.Add(entry.DeepClone());
                continue;
            }

            var scaled = (JObject)entry.DeepClone();
            var scaledQuantity = (JObject)scaled["quantity"]!;
            foreach (var key in QuantityKeys)
            {
                if (quantity[key] is { Type: JTokenType.Integer or JTokenType.Float } value)
                {
                    scaledQuantity[key] = value.Value<float>() * m;
                }
            }
            result.Add(scaled);
        }
        return result;
    }
}

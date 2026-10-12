using System.Collections.Generic;
using GoodHunting.Data;

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
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Probe;

/// <summary>
/// Phase 0/2 diagnostic. Dumps the engine's fully-resolved entity drop + health data
/// (from EntityServerProperties.BehaviorsAsJsonObj) and the fish processed stacks,
/// so the C# port can be generated with guaranteed 1x parity.
/// </summary>
public class ProbeModSystem : ModSystem
{
    static readonly HashSet<string> done = new();

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;
    public override double ExecuteOrder() => 2.0;

    public override void AssetsFinalize(ICoreAPI api) => Guard(api, "assetsfinalize");
    public override void StartServerSide(ICoreServerAPI api)
    {
        Guard(api, "startserverside");
        // Collectible OnLoaded (which resolves processed stacks) runs after StartServerSide; the
        // server asset packet sent to clients is built after WorldReady.
        api.Event.ServerRunPhase(EnumServerRunPhase.WorldReady, () => Guard(api, "worldready"));
    }

    static void Guard(ICoreAPI api, string stage)
    {
        if (!done.Add(stage)) return;
        try { new ProbeModSystem().Dump(api, stage); }
        catch (Exception e) { api.Logger.Error("[probe] EXCEPTION at " + stage + ": " + e); }
    }

    void Dump(ICoreAPI api, string stage)
    {
        var outDir = Environment.GetEnvironmentVariable("GH_DUMP_DIR") ?? "/tmp";
        Directory.CreateDirectory(outDir);
        var sb = new StringBuilder();
        sb.Append("{\n  \"stage\": ").Append(J(stage)).Append(",\n");

        // ---------- entities ----------
        var types = api.World.EntityTypes.OrderBy(t => t.Code.Path, StringComparer.Ordinal).ToList();
        sb.Append("  \"entities\": {\n");
        bool first = true;
        int dropCount = 0;
        foreach (var t in types)
        {
            var code = t.Code.Path;
            var harvest = t.Server.BehaviorsAsJsonObj?.FirstOrDefault(jo => jo?["code"]?.AsString() == "harvestable");
            var health = t.Server.BehaviorsAsJsonObj?.FirstOrDefault(jo => jo?["code"]?.AsString() == "health");
            if (harvest == null && health == null) continue;

            if (!first) sb.Append(",\n");
            first = false;
            sb.Append("    ").Append(J(code)).Append(": {");

            // Which mod an entity belongs to, and what class it is. The key above is the path
            // only (tools/build_data.py depends on that), so without these two a modded animal
            // and a vanilla one with the same path are indistinguishable in the dump.
            sb.Append("\"domain\": ").Append(J(t.Code.Domain ?? ""));
            sb.Append(",\"class\": ").Append(J(t.Class ?? ""));

            sb.Append(",\"drops\": ");
            dropCount += AppendDrops(sb, harvest?["drops"]);

            // What clients receive: the handbook's harvestableDrops attribute.
            sb.Append(",\"attrDrops\": ");
            AppendDrops(sb, t.Attributes?["harvestableDrops"]);

            if (health != null)
            {
                sb.Append(",\"health\": {");
                sb.Append("\"maxhealth\":").Append(F(NullableFloat(health["maxhealth"])));
                sb.Append(",\"currenthealth\":").Append(F(NullableFloat(health["currenthealth"])));
                sb.Append(",\"maxhealthByType\":").Append(Map(health["maxhealthByType"]));
                sb.Append(",\"currenthealthByType\":").Append(Map(health["currenthealthByType"]));
                sb.Append("}");
            }

            // What clients receive: their own health behaviour and the render size.
            var clientHealth = t.Client?.BehaviorsAsJsonObj?.FirstOrDefault(jo => jo?["code"]?.AsString() == "health");
            sb.Append(",\"clientMaxHealth\":").Append(F(NullableFloat(clientHealth?["maxhealth"])));
            sb.Append(",\"clientSize\":").Append(F(t.Client?.Size));
            sb.Append("}");
        }
        sb.Append("\n  },\n");

        // ---------- fish ----------
        sb.Append("  \"fish\": {\n");
        var fishItems = api.World.Collectibles.OfType<CollectibleObject>()
            .Where(c => c.Code != null && c.Code.Path.Contains("fishraw"))
            .OrderBy(c => c.Code.Path, StringComparer.Ordinal)
            .ToList();
        first = true;
        int fishCount = 0;
        foreach (var f in fishItems)
        {
            var gsp = f.CollectibleBehaviors?.OfType<CollectibleBehaviorGroundStoredProcessable>().FirstOrDefault();
            if (gsp == null) continue;
            if (!first) sb.Append(",\n");
            first = false;
            sb.Append("    ").Append(J(f.Code.Path)).Append(": [");
            var ps = gsp.ProcessedStacks;
            if (ps != null)
                for (int i = 0; i < ps.Length; i++)
                {
                    var d = ps[i];
                    if (i > 0) sb.Append(",");
                    sb.Append("{\"type\":").Append(J(d.Type.ToString()))
                      .Append(",\"code\":").Append(J(d.Code?.ToString() ?? ""))
                      .Append(",\"avg\":").Append(F(d.Quantity?.avg))
                      .Append(",\"var\":").Append(F(d.Quantity?.var))
                      .Append("}");
                    fishCount++;
                }
            sb.Append("]");
        }
        sb.Append("\n  },\n");

        // What clients receive for fish: the behaviour properties string sent in the asset packet.
        sb.Append("  \"fishSentToClient\": {\n");
        first = true;
        foreach (var f in fishItems)
        {
            var gsp = f.CollectibleBehaviors?.OfType<CollectibleBehaviorGroundStoredProcessable>().FirstOrDefault();
            if (gsp?.propertiesAtString == null) continue;
            if (!first) sb.Append(",\n");
            first = false;
            sb.Append("    ").Append(J(f.Code.Path)).Append(": ");
            AppendDrops(sb, new JsonObject(JToken.Parse(gsp.propertiesAtString))["processedStacks"]);
        }
        sb.Append("\n  }\n}\n");

        // Fish stacks must be resolved (ResolvedItemstack != null) or processing yields nothing.
        int totalStacks = 0, unresolved = 0;
        foreach (var c in api.World.Collectibles.OfType<CollectibleObject>())
        {
            if (c.Code == null || !c.Code.Path.Contains("fishraw")) continue;
            var g = c.CollectibleBehaviors?.OfType<CollectibleBehaviorGroundStoredProcessable>().FirstOrDefault();
            if (g?.ProcessedStacks == null) continue;
            for (var si = 0; si < g.ProcessedStacks.Length; si++)
            {
                var s = g.ProcessedStacks[si];
                totalStacks++;
                if (s.ResolvedItemstack == null)
                {
                    unresolved++;
                    if (unresolved <= 20)
                        api.Logger.Notification("[probe]   unresolved: parent={0} idx={1} code={2}", c.Code.Path, si, s.Code);
                }
            }
        }
        api.Logger.Notification("[probe] FISH RESOLUTION: totalStacks={0} unresolved={1}", totalStacks, unresolved);

        var path = Path.Combine(outDir, "dump_" + stage + ".json");
        File.WriteAllText(path, sb.ToString());
        api.Logger.Notification("[probe] stage={0} entities={1} dropEntries={2} fishVariants={3} fishEntries={4} -> {5}",
            stage, types.Count, dropCount, fishItems.Count, fishCount, path);
    }

    static float? NullableFloat(JsonObject o) => o == null ? null : o.AsFloat();
    static string F(float? f) => f == null ? "null" : f.Value.ToString("0.####", CultureInfo.InvariantCulture);

    static int AppendDrops(StringBuilder sb, JsonObject drops)
    {
        sb.Append("[");
        var arr = drops?.Exists == true ? drops.AsArray() : Array.Empty<JsonObject>();
        for (int i = 0; i < arr.Length; i++)
        {
            var d = arr[i];
            if (i > 0) sb.Append(",");
            var q = d["quantity"];
            sb.Append("{\"type\":").Append(J(d["type"]?.AsString() ?? "item"))
              .Append(",\"code\":").Append(J(d["code"]?.AsString() ?? ""))
              .Append(",\"avg\":").Append(F(q?["avg"]?.AsFloat()))
              .Append(",\"var\":").Append(F(q?["var"]?.AsFloat()))
              // Raw quantity as the engine kept it: shapes like {offset:1, avg:0} flatten to 0
              // above, which would otherwise look like "drops nothing".
              .Append(",\"q\":").Append(q?.Token == null ? "null" : q.Token.ToString(Newtonsoft.Json.Formatting.None))
              .Append("}");
        }
        sb.Append("]");
        return arr.Length;
    }

    static string Map(JsonObject o)
    {
        if (o == null) return "{}";
        var d = o.AsObject<Dictionary<string, JsonObject>>();
        if (d == null) return "{}";
        return "{" + string.Join(",", d.Select(kv => J(kv.Key) + ":" + F(NullableFloat(kv.Value)))) + "}";
    }

    static string J(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
